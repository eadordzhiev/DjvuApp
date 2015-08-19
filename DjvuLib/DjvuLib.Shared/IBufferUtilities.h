#pragma once

namespace DjvuApp {
	namespace Misc
	{
		ref class IBufferUtilities sealed
		{
		internal:
			static void* GetPointer(Windows::Storage::Streams::IBuffer^ buffer);
		};
	}
}