#include "StringConversion.h"

#include <Windows.h>

using namespace std;

std::wstring utf8_to_utf16(const std::string & utf8)
{
	// Special case of empty input string
	if (utf8.empty())
		return std::wstring();

	// иру 1, Get length (in wchar_t's) of resulting UTF-16 string
	const int utf16_length = ::MultiByteToWideChar(
		CP_UTF8,            // convert from UTF-8
		0,                  // default flags
		utf8.c_str(),        // source UTF-8 string
		utf8.length(),      // length (in chars) of source UTF-8 string
		NULL,               // unused - no conversion done in this step
		0                   // request size of destination buffer, in wchar_t's
		);
	if (utf16_length == 0)
	{
		auto error = GetLastError();
		throw Platform::Exception::CreateException(HRESULT_FROM_WIN32(error));
	}


	// // иру 2, Allocate properly sized destination buffer for UTF-16 string
	std::wstring utf16;
	utf16.resize(utf16_length);

	// // иру 3, Do the actual conversion from UTF-8 to UTF-16
	if (!::MultiByteToWideChar(
		CP_UTF8,            // convert from UTF-8
		0,                  // default flags
		utf8.c_str(),        // source UTF-8 string
		utf8.length(),      // length (in chars) of source UTF-8 string
		&utf16[0],          // destination buffer
		utf16.length()      // size of destination buffer, in wchar_t's
		))
	{
		auto error = GetLastError();
		throw Platform::Exception::CreateException(HRESULT_FROM_WIN32(error));
	}

	return utf16; // ѓ№р!
}

std::string utf16_to_utf8(const wstring & utf16)
{
	auto requiredBufferSize = WideCharToMultiByte(
		CP_UTF8,
		WC_ERR_INVALID_CHARS,
		utf16.c_str(),
		utf16.length(),
		nullptr,
		0,
		nullptr,
		nullptr
		);

	if (requiredBufferSize == 0)
	{
		auto error = GetLastError();
		throw Platform::Exception::CreateException(HRESULT_FROM_WIN32(error));
	}

	requiredBufferSize++;

	std::string buffer(requiredBufferSize, 0);

	auto numBytesWritten = WideCharToMultiByte(
		CP_UTF8,
		WC_ERR_INVALID_CHARS,
		utf16.c_str(),
		utf16.length(),
		const_cast<char *>(buffer.data()),
		requiredBufferSize - 1,
		nullptr,
		nullptr
		);

	if (numBytesWritten != (requiredBufferSize - 1))
	{
		throw Platform::Exception::CreateException(E_UNEXPECTED, L"WideCharToMultiByte returned an unexpected number of bytes written.");
	}

	return buffer;
}

std::wstring stows(std::string s)
{
	std::wstring ws;
	ws.assign(s.begin(), s.end());
	return ws;
}

std::string wstos(std::wstring ws)
{
	std::string s;
	s.assign(ws.begin(), ws.end());
	return s;
}

#ifdef __cplusplus_winrt
Platform::String ^stops(std::string s)
{
	return ref new Platform::String(stows(s).c_str());
}

std::string pstos(Platform::String^ ps)
{
	return wstos(std::wstring(ps->Data()));
}

Platform::String ^atops(const char *text)
{	
	return stops(std::string(text));
}

Platform::String^ utf8tows(const char *text)
{
	return ref new Platform::String(utf8_to_utf16(text).c_str());
}
#endif