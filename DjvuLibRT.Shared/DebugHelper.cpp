#include "pch.h"
#include "DebugHelper.h"

void _Print (int Line, LPCWSTR Function, LPCWSTR File, LPCWSTR Format, ...)
{
    va_list vl;
	va_start (vl, Format);

	WCHAR buffer[255];
	swprintf_s (buffer, L"[%s(%d):%s]: ", File, Line, Function);
	OutputDebugString(buffer);
	vswprintf_s (buffer, Format, vl);
	OutputDebugString(buffer);
	OutputDebugString(L"\n");

	va_end (vl);
}