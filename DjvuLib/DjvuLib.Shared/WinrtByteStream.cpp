#include "pch.h"
#include "WinrtByteStream.h"

#include <ppl.h>

using namespace concurrency;
using namespace std;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;


WinrtByteStream::WinrtByteStream(IRandomAccessStream^ stream) :
	stream(stream),
	dataReader(ref new DataReader(stream))
{
}

WinrtByteStream::~WinrtByteStream()
{
}

template <typename TResult>
TResult performSynchronously(IAsyncOperation<TResult>^ asyncOp)
{
	return task<TResult>(asyncOp).get();
}

size_t WinrtByteStream::read(void *buffer, size_t size)
{
	auto bytesRead = performSynchronously(dataReader->LoadAsync(size));
	if (bytesRead > size)
	{
		throw new exception();
	}
	dataReader->ReadBytes(ArrayReference<uint8>(static_cast<uint8*>(buffer), bytesRead));
	return bytesRead;
}

size_t WinrtByteStream::write(const void *buffer, size_t size)
{
	throw new exception();
}

long WinrtByteStream::tell(void) const
{
	return stream->Position;
}

int WinrtByteStream::seek(long offset, int whence, bool nothrow)
{
	uint64 position;

	switch (whence)
	{
	case SEEK_SET:
		position = offset;
		break;
	case SEEK_CUR:
		position = stream->Position + offset;
		break;
	case SEEK_END:
		position = stream->Size + offset;
		break;
	default:
		throw new exception();
		break;
	}

	stream->Seek(position);

	return 0;
}

void WinrtByteStream::flush(void)
{
	throw new exception();
}