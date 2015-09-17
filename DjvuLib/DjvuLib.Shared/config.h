#pragma once

#include <string>

#define HAVE_NAMESPACES
#define HAVE_EXCEPTIONS
#define HAVE_STDINCLUDES
#define DDJVUAPI
#define MINILISPAPI
#define DEBUGLVL 0

char* getenv(char*);

std::wstring GetWorkingDirectory();

#if WP81

#include <windows.h>

void WINAPI InitializeCriticalSection(_Out_ LPCRITICAL_SECTION lpCriticalSection);

HANDLE WINAPI CreateEvent(
	_In_opt_ LPSECURITY_ATTRIBUTES lpEventAttributes,
	_In_     BOOL                  bManualReset,
	_In_     BOOL                  bInitialState,
	_In_opt_ LPCTSTR               lpName
	);

DWORD WINAPI WaitForSingleObject(
	_In_ HANDLE hHandle,
	_In_ DWORD  dwMilliseconds
	);

#endif