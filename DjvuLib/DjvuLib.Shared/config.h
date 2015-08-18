#pragma once

#include <string>

#define HAVE_EXCEPTIONS
#define HAVE_STDINCLUDES
#define DDJVUAPI
#define MINILISPAPI
#define DEBUGLVL 0

char* getenv(char*);

std::wstring GetWorkingDirectory();