#pragma once
#include "WinRTNativePtr.h"

namespace DjvuApp { namespace Misc 
{
    public ref class IBufferUtilities sealed
    {
    public:
        static WinRTNativePtr GetPointer(Windows::Storage::Streams::IBuffer^ buffer);
    };
} }