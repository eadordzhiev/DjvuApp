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