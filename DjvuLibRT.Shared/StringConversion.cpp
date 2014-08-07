#include "pch.h"
#include "StringConversion.h"

std::wstring utf16_from_utf8(const std::string & utf8)
{
    // Special case of empty input string
if (utf8.empty())
    return std::wstring();

// Шаг 1, Get length (in wchar_t's) of resulting UTF-16 string
const int utf16_length = ::MultiByteToWideChar(
    CP_UTF8,            // convert from UTF-8
    0,                  // default flags
    utf8.data(),        // source UTF-8 string
    utf8.length(),      // length (in chars) of source UTF-8 string
    NULL,               // unused - no conversion done in this step
    0                   // request size of destination buffer, in wchar_t's
    );
if (utf16_length == 0)
{
    // Error
    DWORD error = ::GetLastError();
    throw ;
}


// // Шаг 2, Allocate properly sized destination buffer for UTF-16 string
std::wstring utf16;
utf16.resize(utf16_length);

// // Шаг 3, Do the actual conversion from UTF-8 to UTF-16
if ( ! ::MultiByteToWideChar(
    CP_UTF8,            // convert from UTF-8
    0,                  // default flags
    utf8.data(),        // source UTF-8 string
    utf8.length(),      // length (in chars) of source UTF-8 string
    &utf16[0],          // destination buffer
    utf16.length()      // size of destination buffer, in wchar_t's
    ) )
{
    // не работает сука ... 
    DWORD error = ::GetLastError();
    throw;
}

return utf16; // ура!
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
	return ref new Platform::String(utf16_from_utf8(text).c_str());
}