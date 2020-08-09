#include "PdbSymbolImporter.h"
#include <string>
#include "Util.h"
std::wstring ConvertToWString2(const char* str) {
    size_t cSize = strnlen_s(str, 300) + 1;
    std::wstring wModuleName(cSize, L'#');
    size_t charConvertedCount;
    mbstowcs_s(&charConvertedCount, &wModuleName[0], cSize, str, cSize - 1);
    return wModuleName;
}
bool DoLoadDataFromExe(
    const wchar_t* szFilename,
    IDiaDataSource** ppSource,
    IDiaSession** ppSession,
    IDiaSymbol** ppGlobal)
{
    wchar_t wszExt[MAX_PATH];
    const wchar_t* wszSearchPath = L"SRV**\\\\symbols\\symbols"; // Alternate path to search for debug data
    DWORD dwMachType = 0;
    Log("CoInitialize(NULL)\n");
    HRESULT hr = CoInitialize(NULL);

    // Obtain access to the provider
    Log("CoCreateInstance()\n");
    hr = CoCreateInstance(__uuidof(DiaSource),
        NULL,
        CLSCTX_INPROC_SERVER,
        __uuidof(IDiaDataSource),
        (void**)ppSource);

    if (FAILED(hr)) {
        Log("CoCreateInstance failed - HRESULT = %08X\n", hr);
        wprintf(L"CoCreateInstance failed - HRESULT = %08X\n", hr);
        return false;
    }
    Log("spliting path\n");
    _wsplitpath_s(szFilename, NULL, 0, NULL, 0, NULL, 0, wszExt, MAX_PATH);
    Log("loadDataForExe()\n");
    hr = (*ppSource)->loadDataForExe(szFilename, wszSearchPath, NULL);
    if (FAILED(hr)) {
        Log("loadDataForExe failed - HRESULT = %08X\n", hr);
        wprintf(L"loadDataForExe failed - HRESULT = %08X\n", hr);
        return false;
    }
    Log("openSession\n");
    hr = (*ppSource)->openSession(ppSession);
    if (FAILED(hr)) {
        Log("openSession failed - HRESULT = %08X\n", hr);
        wprintf(L"openSession failed - HRESULT = %08X\n", hr);
        return false;
    }
    Log("get_globalScope\n");
    hr = (*ppSession)->get_globalScope(ppGlobal);
    if (hr != S_OK) {
        Log("get_globalScope failed - HRESULT = %08X\n", hr);
        wprintf(L"get_globalScope failed\n");
        return false;
    }
    return true;
}
bool PdbSymbolImporter::GetRVA(const char* symbolName, DWORD& rva ) {
    if (pGlobalSymbol == NULL) {
        rva = 0;
        return false;
    }
    std::wstring wSymbolName = ConvertToWString2(symbolName);
    IDiaEnumSymbols* pEnumSymbols;
    if (pGlobalSymbol->findChildren(SymTagPublicSymbol, wSymbolName.c_str(), nsNone, &pEnumSymbols) >= 0) {
        IDiaSymbol* pSymbol;
        ULONG celt = 0;
        Log("SymbolNext\n");
        while (pEnumSymbols->Next(1, &pSymbol, &celt) >= 0 && (celt == 1)) {
            if (pSymbol->get_relativeVirtualAddress(&rva) < 0) {
                Log("Error get_relativeVirtualAddress");
            }
            pSymbol->Release();
        }
    }
    else {
        Log("Error finding children");
        return false;
    }
    return true;
}
bool PdbSymbolImporter::LoadFromExe(const char* filePath) {
    if (pDiaDataSource != NULL ||
        pDiaSession != NULL ||
        pGlobalSymbol != NULL) {
        return false;
    }
    std::wstring wFilePath = ConvertToWString2(filePath);
    DoLoadDataFromExe(wFilePath.c_str(), &pDiaDataSource, &pDiaSession, &pGlobalSymbol);
}
PdbSymbolImporter::~PdbSymbolImporter() {
    if (pGlobalSymbol) {
        Log("Cleaning g_pGlobalSymbol\n");
        pGlobalSymbol->Release();
        pGlobalSymbol = NULL;
    }
    if (pDiaSession) {
        Log("Cleaning g_pDiaSession\n");
        pDiaSession->Release();
        pDiaSession = NULL;
    }
    Log("CoUninitialize()\n");
    CoUninitialize();
}