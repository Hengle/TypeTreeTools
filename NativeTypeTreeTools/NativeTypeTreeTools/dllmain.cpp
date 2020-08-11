// dllmain.cpp : Defines the entry point for the DLL application.
#include "framework.h"
#include "dllmain.h"
#include "dia2.h"
#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#include <string>
#include <fstream>
#include "PdbSymbolImporter.h"
#include "Util.h"
#include "Structs.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
typedef void(__cdecl* GenerateTypeTree_t)(Object* object, TypeTree* typeTree, int options);
typedef Object* (__cdecl* Object__Produce_t)(struct RTTIClass* classInfo, struct RTTIClass* classInfo2, int instanceID, int memLabel, ObjectCreationMode mode);
typedef void(__thiscall* TypeTree__TypeTree_t)(TypeTree* self, int memLabel);

GenerateTypeTree_t GenerateTypeTree;
Object__Produce_t Object__Produce;
TypeTree__TypeTree_t TypeTree__TypeTree;

char** CommonString_BufferBegin;
char** CommonString_BufferEnd;
RuntimeTypeArray* gRuntimeTypeArray;

void InitBindings(const char* moduleName) {
    PdbSymbolImporter importer;
    if (!importer.LoadFromExe(moduleName)) {
        return;
    };
    unsigned long long address;
    if (!importer.GetAddress("?BufferBegin@CommonString@Unity@@3QEBDEB", address)) {
        Log("Error getting address CommonString_BufferBegin");
    }
    else {
        Log("Found address CommonString_BufferBegin %p\n", address);
        CommonString_BufferBegin = (char**)address;
    }

    if (!importer.GetAddress("?BufferEnd@CommonString@Unity@@3QEBDEB", address)) {
        Log("Error getting address CommonString_BufferEnd");
    }
    else {
        Log("Found address CommonString_BufferEnd %p\n", address);
        CommonString_BufferEnd = (char**)address;
    }

    if (!importer.GetAddress("?ms_runtimeTypes@RTTI@@0URuntimeTypeArray@1@A", address)) {
        Log("Error getting address RuntimeTypeArray");
    }
    else {
        Log("Found address RuntimeTypeArray %p\n", address);
        gRuntimeTypeArray = (RuntimeTypeArray*)address;
    }

	if (!importer.GetAddress("?GenerateTypeTree@@YAXAEBVObject@@AEAVTypeTree@@W4TransferInstructionFlags@@@Z", address)) {
		Log("Error getting address GenerateTypeTree");
	}
	else {
		Log("Found address GenerateTypeTree %p\n", address);
		GenerateTypeTree = (GenerateTypeTree_t)address;
	}

	if (!importer.GetAddress("?Produce@Object@@CAPEAV1@PEBVType@Unity@@0HUMemLabelId@@W4ObjectCreationMode@@@Z", address)) {
		Log("Error getting address Produce::Object");
	}
	else {
		Log("Found address Produce::Object %p\n", address);
		Object__Produce = (Object__Produce_t)address;
	}

	if (!importer.GetAddress("??0TypeTree@@QEAA@AEBUMemLabelId@@@Z", address)) {
		Log("Error getting address TypeTree::TypeTree");
	}
	else {
		Log("Found address TypeTree::TypeTree %p\n", address);
		TypeTree__TypeTree = (TypeTree__TypeTree_t)address;
	}

}
extern "C" {
#define MEMBER_SIZE(type, field) sizeof(((type *)0)->field)
#define LOG_TYPE(type) Log("%s: %d (0x%x)\n", typeid(type).name(), sizeof(type), sizeof(type))
#define LOG_MEMBER(type, field) Log("    %s: offset %d (0x%x) size %d (0x%x)\n", #field, offsetof(type, field), offsetof(type, field), MEMBER_SIZE(type, field), MEMBER_SIZE(type, field));
    EXPORT void DumpStructDebug() { 
		LOG_TYPE(TypeTree);
		LOG_MEMBER(TypeTree, m_Nodes);
		LOG_MEMBER(TypeTree, m_StringData);
		LOG_MEMBER(TypeTree, m_ByteOffsets);
		Log("\n");

        LOG_TYPE(TypeTreeNode);
        LOG_MEMBER(TypeTreeNode, m_Version);
        LOG_MEMBER(TypeTreeNode, m_Level);
        LOG_MEMBER(TypeTreeNode, m_IsArray);
        LOG_MEMBER(TypeTreeNode, m_TypeStrOffset);
        LOG_MEMBER(TypeTreeNode, m_NameStrOffset);
        LOG_MEMBER(TypeTreeNode, m_ByteSize);
        LOG_MEMBER(TypeTreeNode, m_Index);
        LOG_MEMBER(TypeTreeNode, m_MetaFlag);
        Log("\n");

        LOG_TYPE(MemLabelId);
        LOG_MEMBER(MemLabelId, m_RootReferenceWithSalt);
		LOG_MEMBER(MemLabelId, identifier);
        Log("\n");


        LOG_TYPE(dynamic_array<int>);
        LOG_MEMBER(dynamic_array<int>, data);
        LOG_MEMBER(dynamic_array<int>, label);
        LOG_MEMBER(dynamic_array<int>, size);
        LOG_MEMBER(dynamic_array<int>, cap);
        Log("\n");

        LOG_TYPE(RuntimeTypeArray);
        LOG_MEMBER(RuntimeTypeArray, count);
        LOG_MEMBER(RuntimeTypeArray, Types);
        Log("\n");

        LOG_TYPE(RTTIClass);
        LOG_MEMBER(RTTIClass, base);
        LOG_MEMBER(RTTIClass, factory);
        LOG_MEMBER(RTTIClass, className);
        LOG_MEMBER(RTTIClass, classNamespace);
        LOG_MEMBER(RTTIClass, module);
        LOG_MEMBER(RTTIClass, persistentTypeID);
        LOG_MEMBER(RTTIClass, size);
        LOG_MEMBER(RTTIClass, derivedFromInfo);
        LOG_MEMBER(RTTIClass, isAbstract);
        LOG_MEMBER(RTTIClass, isSealed);
        LOG_MEMBER(RTTIClass, isEditorOnly);
        LOG_MEMBER(RTTIClass, attributes);
		LOG_MEMBER(RTTIClass, attributeCount);
        Log("\n");

        CloseLog();
    }
    EXPORT void ExportStringData(const char* moduleName) {
        InitBindings(moduleName);
        Log("Writing Strings\n");
        if (CommonString_BufferBegin == NULL ||
            CommonString_BufferEnd == NULL) {
            Log("CommonString pointers are null");
        }
        else {
            unsigned long length = *CommonString_BufferEnd - *CommonString_BufferBegin - 1;
            if (length <= 0) {
                Log("Error: string.dat length is %lld", length);
            }
            else {
				CreateDirectory(L"Output", NULL);
                FILE* file = fopen("Output/strings.dat", "wb");
                fwrite(*CommonString_BufferBegin, sizeof(char), length, file);
                fclose(file);
            }
        }
        CloseLog();
    }
    EXPORT void ExportClassesJson(const char* moduleName) {
        Log("ExportClassesJson\n");
		InitBindings(moduleName);
        if (gRuntimeTypeArray != NULL) {
            Log("%d types", gRuntimeTypeArray->count);
			CreateDirectory(L"Output", NULL);
            FILE* json = fopen("Output/classes.json", "w");
            fprintf(json, "{\n");
            for (int i = 0; i < gRuntimeTypeArray->count; i++) {
                auto type = gRuntimeTypeArray->Types[i];
				if (type == NULL) {
					Log("Found NULL pointer for RuntimeType %d\n", i);
				}
                fprintf(json, "    \"%d\": \"%s\"", type->persistentTypeID, type->className);
                if (i < gRuntimeTypeArray->count - 1) {
                    fprintf(json, ",");
                }
                fprintf(json, "\n");
            }
            fprintf(json, "}");
            fclose(json);
        }
        else {
            Log("Error: Could not initialize gRuntimeTypeArray");
        }
        CloseLog();
    }
	EXPORT void ExportStructData(const char* moduleName) {

	}
	EXPORT void ExportStructDump(const char* moduleName) {
		//TODO: Fix UnityPlayer TypeTree struct size is 0x60 bytes, while UnityEditor is 0x78 bytes
		InitBindings(moduleName);
		if (Object__Produce == NULL ||
			TypeTree__TypeTree == NULL ||
			GenerateTypeTree == NULL) {
			Log("Error initializing functions\n");
		}
		if (gRuntimeTypeArray != NULL) {
			CreateDirectory(L"Output", NULL);
			FILE* file = fopen("Output/structs.dump", "w");
			for (int i = 0; i < gRuntimeTypeArray->count; i++) {
				RTTIClass* type = gRuntimeTypeArray->Types[i];
				RTTIClass* iter = type;
				std::string inheritance{};
				while (true) {
					inheritance += iter->className;
					if (iter->base == NULL) break;
					inheritance += " <- ";
					iter = iter->base;
				}
				fprintf(file, "\n// persistentTypeID{%d}: %s\n", type->persistentTypeID, inheritance.c_str());
				iter = type;
				while (iter->isAbstract) {
					fprintf(file, "// %s is abstract\n", iter->className);
					if (iter->base == NULL) break;
					iter = iter->base;
				}
				if (iter == NULL) {
					Log("Error finding concrete type for %d\n", i);
				}
				Object* value = Object__Produce(iter, iter, 0x32, 0, 0);
				TypeTree* typeTree = (TypeTree*)malloc(sizeof(TypeTree));
				TypeTree__TypeTree(typeTree, 0x32);
				GenerateTypeTree(value, typeTree, 0x2000);
				fputs(typeTree->Dump(*CommonString_BufferBegin).c_str(), file);
				fclose(file);
			}
		}
		else {
			Log("Error: Could not initialize gRuntimeTypeArray");
		}
		CloseLog();
	}
}