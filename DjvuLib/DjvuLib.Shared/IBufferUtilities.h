#pragma once

namespace DjvuApp {
	namespace Misc
	{
		class IBufferUtilities abstract sealed
		{
		public:
			static void* GetPointer(Windows::Storage::Streams::IBuffer^ buffer);
			static void GetPointer(Windows::Foundation::IMemoryBufferReference^ reference, void** pointer, UINT32* capacity);
		};
	}
}