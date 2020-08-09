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
        Log("Found address RuntimeTypeArray %llx\n", address);
        gRuntimeTypeArray = (RuntimeTypeArray*)address;
    }

}
extern "C" {
#define MEMBER_SIZE(type, field) sizeof(((type *)0)->field)
#define LOG_TYPE(type) Log("%s: %d (0x%x)\n", typeid(type).name(), sizeof(type), sizeof(type))
#define LOG_MEMBER(type, field) Log("    %s: offset %d (0x%x) size %d (0x%x)\n", #field, offsetof(type, field), offsetof(type, field), MEMBER_SIZE(type, field), MEMBER_SIZE(type, field));
    EXPORT void DumpStructDebug() { 
        LOG_TYPE(Object__RTTI);
        LOG_MEMBER(Object__RTTI, base);
        LOG_MEMBER(Object__RTTI, factory);
        LOG_MEMBER(Object__RTTI, className);
        LOG_MEMBER(Object__RTTI, size);
        LOG_MEMBER(Object__RTTI, isAbstract);
        LOG_MEMBER(Object__RTTI, unk0);
        LOG_MEMBER(Object__RTTI, unk1);
        Log("\n");

        LOG_TYPE(TypeTreeNode);
        LOG_MEMBER(TypeTreeNode, m_Version);
        LOG_MEMBER(TypeTreeNode, m_Depth);
        LOG_MEMBER(TypeTreeNode, m_IsArray);
        LOG_MEMBER(TypeTreeNode, m_Type);
        LOG_MEMBER(TypeTreeNode, m_Name);
        LOG_MEMBER(TypeTreeNode, m_ByteSize);
        LOG_MEMBER(TypeTreeNode, m_Index);
        LOG_MEMBER(TypeTreeNode, m_MetaFlag);
        Log("\n");

        LOG_TYPE(MemLabelId);
        LOG_MEMBER(MemLabelId, id);
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

        LOG_TYPE(RuntimeTypeArray2);
        LOG_MEMBER(RuntimeTypeArray2, count);
        LOG_MEMBER(RuntimeTypeArray2, Types);
        Log("\n");

        LOG_TYPE(RTTIClass);
        LOG_MEMBER(RTTIClass, base);
        LOG_MEMBER(RTTIClass, unk1);
        LOG_MEMBER(RTTIClass, name);
        LOG_MEMBER(RTTIClass, unk3);
        LOG_MEMBER(RTTIClass, unk4);
        LOG_MEMBER(RTTIClass, classID);
        LOG_MEMBER(RTTIClass, objectSize);
        LOG_MEMBER(RTTIClass, typeIndex);
        LOG_MEMBER(RTTIClass, unk5);
        LOG_MEMBER(RTTIClass, isAbstract);
        LOG_MEMBER(RTTIClass, unk6);
        LOG_MEMBER(RTTIClass, unk7);
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
                FILE* file = fopen("strings.dat", "wb");
                fwrite(*CommonString_BufferBegin, sizeof(char), length, file);
                fclose(file);
            }
        }
        CloseLog();
    }
    EXPORT void ExportClassesJson(const char* moduleName) {
        Log("ExportClassesJson\n");
        InitBindings(moduleName);
        //TODO: FIX
        if (gRuntimeTypeArray != NULL) {
            Log("%d types", gRuntimeTypeArray->count);
            FILE* json = fopen("classes.json", "w");
            fprintf(json, "{\n");
            for (int i = 0; i < gRuntimeTypeArray->count; i++) {
                auto type = gRuntimeTypeArray->Types[i];
                if (type == NULL) continue;
                fprintf(json, "    \"%d\": \"%s\"", type->classID, type->name);
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
}