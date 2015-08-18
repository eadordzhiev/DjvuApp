#pragma once
#include <string>

std::wstring utf8_to_utf16(const std::string & utf8);

std::string utf16_to_utf8(const std::wstring & utf16);

#ifdef __cplusplus_winrt
Platform::String^ utf8_to_ps(const std::string & utf8);
#endif