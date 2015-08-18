#include "StringConversion.h"

#include <Windows.h>

using namespace std;
using namespace Platform;

wstring utf8_to_utf16(const string & utf8)
{
	if (utf8.empty())
	{
		return wstring();
	}

	auto utf16_length = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), utf8.length(), nullptr, 0);
	if (utf16_length == 0)
	{
		auto error = GetLastError();
		throw Exception::CreateException(HRESULT_FROM_WIN32(error));
	}
	
	wstring utf16;
	utf16.resize(utf16_length);

	if (!MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), utf8.length(), &utf16[0], utf16.length()))
	{
		auto error = GetLastError();
		throw Exception::CreateException(HRESULT_FROM_WIN32(error));
	}

	return utf16;
}

string utf16_to_utf8(const wstring & utf16)
{
	if (utf16.empty())
	{
		return string();
	}

	auto utf8_length = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, utf16.c_str(), utf16.length(), nullptr, 0, nullptr, nullptr);
	if (utf8_length == 0)
	{
		auto error = GetLastError();
		throw Platform::Exception::CreateException(HRESULT_FROM_WIN32(error));
	}

	string utf8;
	utf8.resize(utf8_length);

	if (!WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, utf16.c_str(), utf16.length(), &utf8[0], utf8.length(), nullptr, nullptr))
	{
		auto error = GetLastError();
		throw Exception::CreateException(HRESULT_FROM_WIN32(error));
	}

	return utf8;
}

String^ utf8_to_ps(const string & utf8)
{
	auto utf16 = utf8_to_utf16(utf8);
	return ref new String(utf16.c_str(), utf16.length());
}