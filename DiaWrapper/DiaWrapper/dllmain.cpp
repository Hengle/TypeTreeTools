// dllmain.cpp : Defines the entry point for the DLL application.
#include "dllmain.h"
#include "dia2.h"
#include "PdbSymbolImporter.h"

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
    EXPORT PdbSymbolImporter* CreateImporter() {
        return new PdbSymbolImporter();
    }
    EXPORT bool GetRVA(PdbSymbolImporter* importer, const char* symbolName, DWORD& rva) {
        return importer->GetRVA(symbolName, rva);
    }
    EXPORT bool GetAddress(PdbSymbolImporter* importer, const char* symbolName, unsigned long long& addr) {
        return importer->GetAddress(symbolName, addr);
    }
    EXPORT void FreeImporter(PdbSymbolImporter* importer) {
        delete importer;
    }
}