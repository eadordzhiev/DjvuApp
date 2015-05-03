#include "pch.h"
#include "DebugHelper.h"

void printDebugMessage(int lineNumber, LPCWSTR functionName, LPCWSTR fileName, LPCWSTR format, ...)
{
    va_list vl;
    va_start(vl, format);

    std::wstringstream stream;
    //stream << L"[" << fileName << L"(" << lineNumber << L"):" << functionName << L"]: ";

    std::wstring str = stream.str();
    OutputDebugString(str.c_str());

    WCHAR buffer[1024];
    vswprintf_s(buffer, format, vl);
    OutputDebugString(buffer);

    OutputDebugString(L"\n");

	va_end (vl);
}