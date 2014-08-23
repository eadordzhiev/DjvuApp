// DjvuLibRT.cpp
#include "pch.h"
#include "DjvuLibRT.h"
#include <collection.h>
#include <ppltasks.h>

using namespace concurrency;
using namespace std;

using namespace Microsoft::WRL;
using namespace Platform;
using namespace DjvuLibRT;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Storage::Streams;

typedef unsigned int uint;

IAsyncOperation<DjvuDocument^>^ DjvuDocument::LoadAsync(String^ path)
{
	return create_async([path]() -> DjvuDocument^
	{
		auto utf8_path = ConvertCxStringToUTF8(path);
		auto result = ref new DjvuDocument(utf8_path.c_str());
		return result;
	});
}

ddjvu_format_t* DjvuDocument::GetFormat()
{
	if (format == nullptr)
	{
		throw ref new Exception(E_FAIL, "Format has been released");
	}

	return format;
}

DjvuDocument::DjvuDocument(const char* path)
{
	context = ddjvu_context_create(nullptr);
	format = ddjvu_format_create(DDJVU_FORMAT_BGRA, 0, 0);
	ddjvu_format_set_row_order(format, 1);
		
	document = ddjvu_document_create_by_filename_utf8(context, path, false);
	if (document == nullptr)
	{
		throw ref new Exception(E_FAIL, "Can't open the document");
	}

#if THREADMODEL != NOTHREADS
	while (!ddjvu_document_decoding_done(document))
		Sleep(1);
#endif

	pageCount = ddjvu_document_get_pagenum(document);
	GP<DjVuDocument> djvuDoc = ddjvu_get_DjVuDocument(document);
	doctype = static_cast<DocumentType>(djvuDoc->get_doc_type());
}

DjvuDocument::~DjvuDocument()
{
	if (context != nullptr)
	{
		ddjvu_context_release(context);
		context = nullptr;
	}
	if (format != nullptr)
	{
		ddjvu_format_release(format);
		format = nullptr;
	}
	if (document != nullptr)
	{
		ddjvu_document_release(document);
		document = nullptr;
	}
}

Platform::Array<PageInfo>^ DjvuDocument::GetPageInfos()
{
	ddjvu_pageinfo_t info;
	auto result = ref new Platform::Array<PageInfo>(pageCount);

	for (unsigned int i = 0; i < pageCount; i++)
	{
		ddjvu_document_get_pageinfo(document, i, &info);
		result[i].Width = info.width;
		result[i].Height = info.height;
		result[i].Dpi = info.dpi;
		result[i].PageNumber = i + 1;
	}

	return result;
}

struct DjvuBookmarkComparator : public std::binary_function<const DjvuBookmark, const DjvuBookmark, bool>
{
	bool operator()(const DjvuBookmark& _Left, const DjvuBookmark& _Right) const
	{
		return _Left.Url == _Right.Url;
	};
};

Array<DjvuBookmark>^ DjvuDocument::GetBookmarks()
{
	GP<DjVuDocument> djvuDoc = ddjvu_get_DjVuDocument(document);
	GP<DjVmNav> navm = djvuDoc->get_djvm_nav();

	if (navm == nullptr)
		return nullptr;

	int count = navm->getBookMarkCount();
	auto result = ref new Array<DjvuBookmark>(count);

	for (int i = 0; i < count; i++)
	{
		GP<DjVmNav::DjVuBookMark> bookmark;
		bool success = navm->getBookMark(bookmark, i);

		if (!success)
		{
			DBGPRINT(L"Can't get bookmark at index %d, getBookMark() returned false", i);
			throw ref new Exception(E_FAIL, "getBookMark() failed");
		}

		result[i].Name = utf8tows(bookmark->displayname);
		result[i].Url = utf8tows(bookmark->url);
		result[i].ChildrenCount = bookmark->count;
	}

	return result;
}

IAsyncOperation<DjvuPage^>^ DjvuDocument::GetPageAsync(uint pageNumber)
{
	if (pageNumber < 1 || pageNumber > pageCount)
	{
		throw ref new InvalidArgumentException("Pageno is out of range");
	}

	return create_async([this, pageNumber]() -> DjvuPage^
	{
		ddjvu_page_t* page = ddjvu_page_create_by_pageno(document, pageNumber - 1);
		if (page == nullptr)
		{
			throw ref new Exception(E_FAIL, "Cannot get page");
		}

#if THREADMODEL != NOTHREADS
		while (!ddjvu_page_decoding_done(page))
			Sleep(1);
#endif

		return ref new DjvuPage(page, this, pageNumber);
	});
}

DjvuPage::DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint pageNumber)
{
	this->page = page;
	this->document = document;
	this->width = ddjvu_page_get_width(page);
	this->height = ddjvu_page_get_height(page);
	this->pageNumber = pageNumber;
}

DjvuPage::~DjvuPage()
{
	document = nullptr;

	if (page != nullptr)
	{
		ddjvu_page_release(page);
		page = nullptr;
	}
}

IAsyncAction^ DjvuPage::RenderRegionAsync(WriteableBitmap^ bitmap, Size rescaledPageSize, Rect renderRegion)
{
	if (page == nullptr)
		throw ref new ObjectDisposedException();

	DBGPRINT(L"width = %f, height = %f", rescaledPageSize.Width, rescaledPageSize.Height);
	if (rescaledPageSize.Width < 1 || rescaledPageSize.Height < 1)
	{
		throw ref new InvalidArgumentException("Width or height is out of range");
	}

	ddjvu_rect_t prect;
	ddjvu_rect_t rrect;
	size_t rowsize;

	/* Process segment specification */
	rrect.x = (int) renderRegion.X;
	rrect.y = (int) renderRegion.Y;
	rrect.w = (uint) renderRegion.Width;
	rrect.h = (uint) renderRegion.Height;

	/* Process size specification */
	prect.x = 0;
	prect.y = 0;
	prect.w = (uint) rescaledPageSize.Width;
	prect.h = (uint) rescaledPageSize.Height;

	rowsize = rrect.w * 4;

	byte* pDstPixels;
	auto buffer = bitmap->PixelBuffer;
	// Obtain IBufferByteAccess
	ComPtr<IBufferByteAccess> pBufferByteAccess;
	ComPtr<IUnknown> pBuffer((IUnknown*)buffer);
	pBuffer.As(&pBufferByteAccess);

	// Get pointer to pixel bytes
	pBufferByteAccess->Buffer(&pDstPixels);

	if (buffer->Length < rrect.w * rrect.h)
	{
		throw ref new InvalidArgumentException("Buffer is too small");
	}

	return create_async([this, prect, rrect, pDstPixels, rowsize]
	{
		/* Render */
		if (!ddjvu_page_render(page, DDJVU_RENDER_COLOR, &prect, &rrect, document->GetFormat(), rowsize, (char*)pDstPixels))
		{
			DBGPRINT(L"Cannot render page, no data");
			memset(pDstPixels, UINT_MAX, rowsize * rrect.h);
		}
	});
}