#pragma once

#ifdef _WIN64
typedef uint64 WinRTNativePtr;
#else
typedef uint32 WinRTNativePtr;
#endif