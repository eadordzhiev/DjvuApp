#include "libdjvu\ByteStream.h"

#pragma once
class WinrtByteStream : public ByteStream::Wrapper
{
public:
	WinrtByteStream(Windows::Storage::Streams::IRandomAccessStream^ stream);
	~WinrtByteStream();

	virtual size_t read(void *buffer, size_t size) override;
	virtual size_t write(const void *buffer, size_t size) override;
	virtual long tell(void) const override;
	virtual int seek(long offset, int whence = SEEK_SET, bool nothrow = false) override;
	virtual void flush(void) override;
private:
	Windows::Storage::Streams::IRandomAccessStream^ stream;
	Windows::Storage::Streams::DataReader^ dataReader;
};

