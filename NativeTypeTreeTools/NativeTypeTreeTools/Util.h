#pragma once
#include <Windows.h>
#include <stdio.h>
#include <stdlib.h>
static FILE* g_Log;
static void Log(const char* fmt, ...) {
    if (g_Log == NULL) {
        g_Log = fopen("NativeTypeTreeToolsLog.txt", "w");
    }
    va_list argp;
    va_start(argp, fmt);
    vfprintf(g_Log, fmt, argp);
    fflush(g_Log);
    va_end(argp);
}
static void CloseLog(){
    if (g_Log != NULL) {
        fclose(g_Log);
        g_Log = NULL;
    }
}