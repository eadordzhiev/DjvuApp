void _Print (int Line, LPCWSTR Function, LPCWSTR File, LPCWSTR Format, ...);

#if _DEBUG
#define DBGPRINT(Format, ...) _Print (__LINE__, __FUNCTIONW__, __FILEW__, Format, __VA_ARGS__)
#else
#define DBGPRINT(Format, ...)
#endif