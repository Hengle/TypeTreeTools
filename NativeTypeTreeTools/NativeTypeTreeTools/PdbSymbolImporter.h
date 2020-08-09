#pragma once
#include "dia2.h"
class PdbSymbolImporter {
	IDiaDataSource* pDiaDataSource;
	IDiaSession* pDiaSession;
	IDiaSymbol* pGlobalSymbol;
public:
	bool LoadFromExe(const char* exePath);
	bool GetRVA(const char* symbolPath, DWORD& rva);
	~PdbSymbolImporter();
};