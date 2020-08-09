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
    EXPORT void DumpStructDebug() { 
        Log("%s: %d (0x%x)\n", typeid(Object__RTTI).name(), sizeof(Object__RTTI), sizeof(Object__RTTI));
        Log("    %s: %d (0x%x)\n", "base", offsetof(Object__RTTI, base), offsetof(Object__RTTI, base));
        Log("    %s: %d (0x%x)\n", "factory", offsetof(Object__RTTI, factory), offsetof(Object__RTTI, factory));
        Log("    %s: %d (0x%x)\n", "className", offsetof(Object__RTTI, className), offsetof(Object__RTTI, className));
        Log("    %s: %d (0x%x)\n", "size", offsetof(Object__RTTI, size), offsetof(Object__RTTI, size));
        Log("    %s: %d (0x%x)\n", "isAbstract", offsetof(Object__RTTI, isAbstract), offsetof(Object__RTTI, isAbstract));
        Log("    %s: %d (0x%x)\n", "unk0", offsetof(Object__RTTI, unk0), offsetof(Object__RTTI, unk0));
        Log("    %s: %d (0x%x)\n", "unk1", offsetof(Object__RTTI, unk1), offsetof(Object__RTTI, unk1));
        Log("\n");

        Log("%s: %d (0x%x)\n", typeid(TypeTreeNode).name(), sizeof(TypeTreeNode), sizeof(TypeTreeNode));
        Log("    %s: %d (0x%x)\n", "m_Version", offsetof(TypeTreeNode, m_Version), offsetof(TypeTreeNode, m_Version));
        Log("    %s: %d (0x%x)\n", "m_Depth", offsetof(TypeTreeNode, m_Depth), offsetof(TypeTreeNode, m_Depth));
        Log("    %s: %d (0x%x)\n", "m_IsArray", offsetof(TypeTreeNode, m_IsArray), offsetof(TypeTreeNode, m_IsArray));
        Log("    %s: %d (0x%x)\n", "m_Type", offsetof(TypeTreeNode, m_Type), offsetof(TypeTreeNode, m_Type));
        Log("    %s: %d (0x%x)\n", "m_Name", offsetof(TypeTreeNode, m_Name), offsetof(TypeTreeNode, m_Name));
        Log("    %s: %d (0x%x)\n", "m_ByteSize", offsetof(TypeTreeNode, m_ByteSize), offsetof(TypeTreeNode, m_ByteSize));
        Log("    %s: %d (0x%x)\n", "m_Index", offsetof(TypeTreeNode, m_Index), offsetof(TypeTreeNode, m_Index));
        Log("    %s: %d (0x%x)\n", "m_MetaFlag", offsetof(TypeTreeNode, m_MetaFlag), offsetof(TypeTreeNode, m_MetaFlag));
        Log("\n");

        Log("%s: %d (0x%x)\n", typeid(MemLabelId).name(), sizeof(MemLabelId), sizeof(MemLabelId));
        Log("    %s: %d (0x%x)\n", "id", offsetof(MemLabelId, id), offsetof(MemLabelId, id));
        Log("\n");

        Log("%s: %d (0x%x)\n", typeid(dynamic_array<int>).name(), sizeof(dynamic_array<int>), sizeof(dynamic_array<int>));
        Log("    %s: %d (0x%x)\n", "data", offsetof(dynamic_array<int>, data), offsetof(dynamic_array<int>, data));
        Log("    %s: %d (0x%x)\n", "label", offsetof(dynamic_array<int>, label), offsetof(dynamic_array<int>, label));
        Log("    %s: %d (0x%x)\n", "size", offsetof(dynamic_array<int>, size), offsetof(dynamic_array<int>, size));
        Log("    %s: %d (0x%x)\n", "cap", offsetof(dynamic_array<int>, cap), offsetof(dynamic_array<int>, cap));
        Log("\n");

        Log("%s: %d (0x%x)\n", typeid(RuntimeTypeArray).name(), sizeof(RuntimeTypeArray), sizeof(RuntimeTypeArray));
        Log("    %s: %d (0x%x)\n", "count", offsetof(RuntimeTypeArray, count), offsetof(RuntimeTypeArray, count));
        Log("    %s: %d (0x%x)\n", "Types", offsetof(RuntimeTypeArray, Types), offsetof(RuntimeTypeArray, Types));
        Log("\n");

        Log("%s: %d (0x%x)\n", typeid(RTTIClass).name(), sizeof(RTTIClass), sizeof(RTTIClass));
        Log("    %s: %d (0x%x)\n", "base", offsetof(RTTIClass, base), offsetof(RTTIClass, base));
        Log("    %s: %d (0x%x)\n", "unk1", offsetof(RTTIClass, unk1), offsetof(RTTIClass, unk1));
        Log("    %s: %d (0x%x)\n", "name", offsetof(RTTIClass, name), offsetof(RTTIClass, name));
        Log("    %s: %d (0x%x)\n", "unk3", offsetof(RTTIClass, unk3), offsetof(RTTIClass, unk3));
        Log("    %s: %d (0x%x)\n", "unk4", offsetof(RTTIClass, unk4), offsetof(RTTIClass, unk4));
        Log("    %s: %d (0x%x)\n", "classID", offsetof(RTTIClass, classID), offsetof(RTTIClass, classID));
        Log("    %s: %d (0x%x)\n", "objectSize", offsetof(RTTIClass, objectSize), offsetof(RTTIClass, objectSize));
        Log("    %s: %d (0x%x)\n", "typeIndex", offsetof(RTTIClass, typeIndex), offsetof(RTTIClass, typeIndex));
        Log("    %s: %d (0x%x)\n", "unk5", offsetof(RTTIClass, unk5), offsetof(RTTIClass, unk5));
        Log("    %s: %d (0x%x)\n", "isAbstract", offsetof(RTTIClass, isAbstract), offsetof(RTTIClass, isAbstract));
        Log("    %s: %d (0x%x)\n", "unk6", offsetof(RTTIClass, unk6), offsetof(RTTIClass, unk6));
        Log("    %s: %d (0x%x)\n", "unk7", offsetof(RTTIClass, unk7), offsetof(RTTIClass, unk7));
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