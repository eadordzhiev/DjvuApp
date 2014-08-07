std::wstring utf16_from_utf8(const std::string & utf8);

std::wstring stows(std::string s);

std::string wstos(std::wstring ws);

Platform::String ^stops(std::string s);

std::string pstos(Platform::String^ ps);

Platform::String ^atops(const char *text);

Platform::String^ utf8tows(const char *text);