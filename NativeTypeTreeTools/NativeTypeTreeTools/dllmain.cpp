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

        CloseLog();
    }
    EXPORT DWORD GetRVA(const char* moduleName, const char* symbolName) {
        DWORD address = 0;
        {
            PdbSymbolImporter importer;
            if (!importer.LoadFromExe(moduleName)) {
                goto cleanup;
            };
            importer.GetRVA(symbolName, address);
        }
    cleanup:
        CloseLog();
        return address;
    }
}