#include "pch.h"
#include "IBufferUtilities.h"

using namespace Microsoft::WRL;
using namespace Platform;
using namespace Windows::Storage::Streams;
using namespace DjvuApp::Misc;

void ThrowIfFailed(HRESULT hr)
{
	if (FAILED(hr))
	{
		throw ref new Exception(hr);
	}
}

void* IBufferUtilities::GetPointer(IBuffer^ buffer)
{
	ComPtr<IInspectable> inspectable(reinterpret_cast<IInspectable*>(buffer));

	ComPtr<IBufferByteAccess> bufferByteAccess;
	ThrowIfFailed(inspectable.As(&bufferByteAccess));

	byte* pointer = nullptr;
	ThrowIfFailed(bufferByteAccess->Buffer(&pointer));

	return pointer;
}