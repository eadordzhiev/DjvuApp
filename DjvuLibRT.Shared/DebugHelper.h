void printDebugMessage(int lineNumber, LPCWSTR functionName, LPCWSTR fileName, LPCWSTR format, ...);

#if _DEBUG
#define DBGPRINT(format, ...) _Print (__LINE__, __FUNCTIONW__, __FILEW__, format, __VA_ARGS__)
#else
#define DBGPRINT(format, ...)
#endif