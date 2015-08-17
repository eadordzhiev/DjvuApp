#include "pch.h"
#include "DjvuInterop.h"
#include "LicenseValidator.h"
#include "IBufferUtilities.h"

using namespace concurrency;
using namespace std;

using namespace Microsoft::WRL;
using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Windows::Graphics::Imaging;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace DjvuApp;
using namespace DjvuApp::Djvu;
using namespace DjvuApp::Misc;

static void RethrowToWinRtException(const GException& ex)
{
	auto functionName = ex.get_function();
	if (functionName == nullptr)
	{
		functionName = "?";
	}

	auto cause = ex.get_cause();
	if (cause == nullptr)
	{
		cause = "?";
	}

	auto fileName = ex.get_file();
	if (fileName == nullptr)
	{
		fileName = "?";
	}

	std::wstringstream message;
	message << L"Exception in function " << utf16_from_utf8(functionName) << std::endl;
	message << L"Message: " << utf16_from_utf8(cause) << std::endl;
	message << L"In file " << utf16_from_utf8(fileName) << std::endl;
	message << L"At line " << ex.get_line();
    
	throw ref new FailureException(ref new String(message.str().c_str()));
}

IAsyncOperation<DjvuDocument^>^ DjvuDocument::LoadAsync(String^ path)
{
	return create_async([path]()
	{
        return LicenseValidator::GetLicenseStatusStealthily()
			.then([=](bool isLicenseValid)
		{
			if (!isLicenseValid)
			{
				throw ref new Exception(E_UNEXPECTED);
			}

			auto utf8_path = ConvertCxStringToUTF8(path);
			return ref new DjvuDocument(utf8_path.c_str());
		});
	});
}

IAsyncOperation<DjvuDocument^>^ DjvuDocument::LoadAsync(IStorageFile^ file)
{
    return DjvuDocument::LoadAsync(file->Path);
}

DjvuDocument::DjvuDocument(const char* path)
{
	try
	{
		context = ddjvu_context_create(nullptr);
		document = ddjvu_document_create_by_filename_utf8(context, path, false);
		
		auto djvuDocument = ddjvu_get_DjVuDocument(document);
		if (!djvuDocument->wait_for_complete_init())
		{
			throw ref new FailureException(L"Failed to decode the document.");
		}

		doctype = static_cast<DocumentType>(djvuDocument->get_doc_type());
        if (doctype != DocumentType::SinglePage && doctype != DocumentType::Bundled)
        {
            throw ref new InvalidArgumentException("Unsupported document type. Only bundled and single page documents are supported.");
        }

		pageCount = djvuDocument->get_pages_num();
		pageInfos = ref new Platform::Array<PageInfo>(pageCount);
		
		for (unsigned int i = 0; i < pageCount; i++)
		{
			ddjvu_pageinfo_t ddjvuinfo;
			ddjvu_document_get_pageinfo(document, i, &ddjvuinfo);

			PageInfo pageInfo;
			pageInfo.Width = ddjvuinfo.width;
			pageInfo.Height = ddjvuinfo.height;
			pageInfo.Dpi = ddjvuinfo.dpi;
			pageInfo.PageNumber = i + 1;

			pageInfos[i] = pageInfo;
		}
	}
	catch (const GException& ex)
	{
		RethrowToWinRtException(ex);
	}
}

DjvuDocument::~DjvuDocument()
{
	if (context != nullptr)
	{
		ddjvu_context_release(context);
		context = nullptr;
	}
	if (document != nullptr)
	{
		ddjvu_document_release(document);
		document = nullptr;
	}
}

Platform::Array<PageInfo>^ DjvuDocument::GetPageInfos()
{
	return ref new Array<PageInfo>(pageInfos);
}

Array<DjvuBookmark>^ DjvuDocument::GetBookmarks()
{
	try
	{
		auto djvuDoc = ddjvu_get_DjVuDocument(document);
		auto navm = djvuDoc->get_djvm_nav();

		if (navm == nullptr)
			return nullptr;

		auto count = navm->getBookMarkCount();
		auto result = ref new Array<DjvuBookmark>(count);

		for (int i = 0; i < count; i++)
		{
			GP<DjVmNav::DjVuBookMark> bookmark;
			if (!navm->getBookMark(bookmark, i))
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
	catch (const GException& ex)
	{
		RethrowToWinRtException(ex);
	}
}

DjvuPage^ DjvuDocument::GetPage(uint32 pageNumber)
{
	if (pageNumber < 1 || pageNumber > pageCount)
	{
		throw ref new InvalidArgumentException("pageNumber is out of range");
	}
    
	ddjvu_page_t* page;

	try
	{
		page = ddjvu_page_create_by_pageno(document, pageNumber - 1);

		auto djvuImage = ddjvu_get_DjVuImage(page);
		if (!djvuImage->wait_for_complete_decode())
		{
			throw ref new FailureException("Failed to decode the page.");
		}
	}
	catch (const GException& ex)
	{
		RethrowToWinRtException(ex);
	}

    assert(page != nullptr);

	return ref new DjvuPage(page, this, pageNumber);
}

IAsyncOperation<DjvuPage^>^ DjvuDocument::GetPageAsync(uint32 pageNumber)
{
	return create_async([=]() -> DjvuPage^
	{
		return GetPage(pageNumber);
	});
}

DjvuPage::DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint32 pageNumber)
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

void DjvuPage::RenderRegion(void* bufferPtr, Size rescaledPageSize, Rect renderRegion)
{
	if (page == nullptr)
	{
		throw ref new ObjectDisposedException();
	}

	DBGPRINT(L"width = %f, height = %f", rescaledPageSize.Width, rescaledPageSize.Height);
	if (rescaledPageSize.Width < 1 || rescaledPageSize.Height < 1)
	{
		throw ref new InvalidArgumentException("The dimensions are out of the range.");
	}

	ddjvu_rect_t prect;
	ddjvu_rect_t rrect;
	size_t rowsize;

	rrect.x = (int)renderRegion.X;
	rrect.y = (int)renderRegion.Y;
	rrect.w = (unsigned int)renderRegion.Width;
	rrect.h = (unsigned int)renderRegion.Height;

	prect.x = 0;
	prect.y = 0;
	prect.w = (unsigned int)rescaledPageSize.Width;
	prect.h = (unsigned int)rescaledPageSize.Height;

	rowsize = rrect.w * 4;

	auto format = ddjvu_format_create(ddjvu_format_style_t::DDJVU_FORMAT_BGRA, 0, nullptr);
	ddjvu_format_set_row_order(format, 1);
	ddjvu_format_set_y_direction(format, 1);

	try
	{
		if (!ddjvu_page_render(page, DDJVU_RENDER_COLOR, &prect, &rrect, format, rowsize, (char*)bufferPtr))
		{
			DBGPRINT(L"Cannot render page, no data.");
			memset(bufferPtr, UINT_MAX, rowsize * rrect.h);
		}
	}
	catch (const GException& ex)
	{
		RethrowToWinRtException(ex);
	}

	ddjvu_format_release(format);
}

IAsyncAction^ DjvuPage::RenderRegionAsync(WriteableBitmap^ bitmap, Size rescaledPageSize, Rect renderRegion)
{
    auto buffer = bitmap->PixelBuffer;
    
	if (buffer->Length < renderRegion.Width * renderRegion.Height)
	{
		throw ref new InvalidArgumentException("Buffer is too small.");
	}

    auto bufferPtr = IBufferUtilities::GetPointer(buffer);

	return create_async([=]
	{
        RenderRegion(bufferPtr, rescaledPageSize, renderRegion);
	});
}