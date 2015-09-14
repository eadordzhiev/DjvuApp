#include "config.h"

char * getenv(char *)
{
	return nullptr;
}

std::wstring GetWorkingDirectory()
{
	auto path = Windows::ApplicationModel::Package::Current->InstalledLocation->Path;
	std::wstring result(begin(path), end(path));
	return result;
}

#if WP81

void WINAPI InitializeCriticalSection(_Out_ LPCRITICAL_SECTION lpCriticalSection)
{
	InitializeCriticalSectionEx(lpCriticalSection, 4000, 0);
}

HANDLE WINAPI CreateEvent(
	_In_opt_ LPSECURITY_ATTRIBUTES lpEventAttributes,
	_In_     BOOL                  bManualReset,
	_In_     BOOL                  bInitialState,
	_In_opt_ LPCTSTR               lpName
	)
{
	DWORD dwFlags = 0;
	if (bManualReset)
	{
		dwFlags |= CREATE_EVENT_MANUAL_RESET;
	}
	if (bInitialState)
	{
		dwFlags |= CREATE_EVENT_INITIAL_SET;
	}

	return CreateEventEx(lpEventAttributes, lpName, dwFlags, EVENT_ALL_ACCESS);
}

DWORD WINAPI WaitForSingleObject(
	_In_ HANDLE hHandle,
	_In_ DWORD  dwMilliseconds
	)
{
	return WaitForSingleObjectEx(hHandle, dwMilliseconds, false);
}

#endif