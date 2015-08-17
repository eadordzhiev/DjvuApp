#pragma once
#include <string>

std::wstring utf8_to_utf16(const std::string & utf8);

std::string utf16_to_utf8(const std::wstring & utf16);

std::wstring stows(std::string s);

std::string wstos(std::wstring ws);

#ifdef __cplusplus_winrt
Platform::String ^stops(std::string s);

std::string pstos(Platform::String^ ps);

Platform::String ^atops(const char *text);

Platform::String^ utf8tows(const char *text);
#endif