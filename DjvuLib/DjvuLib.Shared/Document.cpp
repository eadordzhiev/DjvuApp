#include "pch.h"
#include "Document.h"

#include "libdjvu\DjVuImage.h"
#include "libdjvu\DjVuDocument.h"
#include "libdjvu\DjVmNav.h"
#include "libdjvu\DjVuText.h"
#include "libdjvu\ddjvuapi.h"
#include "WinrtByteStream.h"

using namespace concurrency;
using namespace std;

using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace DjvuApp::Djvu;

IAsyncOperation<DjvuDocument^>^ DjvuDocument::LoadAsync(IStorageFile^ file)
{
	return create_async([=]
	{
		return create_task(file->OpenReadAsync())
			.then([file](IRandomAccessStreamWithContentType^ stream)
		{
			return ref new DjvuDocument(stream);
		}, task_continuation_context::use_arbitrary());
	});
}

DjvuDocument::DjvuDocument(IRandomAccessStream^ stream)
{
	GP<ByteStream> bs = new WinrtByteStream(stream);

	context = ddjvu_context_create(nullptr);
	document = ddjvu_document_create_by_bytestream(context, bs, false);
	
	if (document == nullptr || ddjvu_document_decoding_error(document))
	{
		throw ref new FailureException(L"Failed to decode the document.");
	}

	auto djvuDocument = ddjvu_get_DjVuDocument(document);
	auto doctype = djvuDocument->get_doc_type();

	if (doctype != DjVuDocument::DOC_TYPE::SINGLE_PAGE && doctype != DjVuDocument::DOC_TYPE::BUNDLED)
	{
		throw ref new NotImplementedException("Unsupported document type. Only bundled and single page documents are supported.");
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

		pageInfos[i] = pageInfo;
	}
}

DjvuDocument::~DjvuDocument()
{
	if (document != nullptr)
	{
		ddjvu_document_release(document);
		document = nullptr;
	}
	if (context != nullptr)
	{
		ddjvu_context_release(context);
		context = nullptr;
	}
}

Array<PageInfo>^ DjvuDocument::GetPageInfos()
{
	return ref new Array<PageInfo>(pageInfos);
}

IAsyncOperation<IVectorView<DjvuOutlineItem^>^>^ DjvuDocument::GetOutlineAsync()
{
	return create_async([=]
	{
		return DjvuOutlineItem::GetOutline(document);
	});
}

IAsyncOperation<TextLayerZone^>^ DjvuDocument::GetTextLayerAsync(uint32_t pageNumber)
{
	return create_async([=]
	{
		return TextLayerZone::GetTextLayer(document, pageNumber);
	});
}

DjvuPage^ DjvuDocument::GetPage(uint32_t pageNumber)
{
	auto page = ddjvu_page_create_by_pageno(document, pageNumber - 1);
	assert(page != nullptr);
	
	if (ddjvu_page_decoding_error(page))
	{
		throw ref new FailureException("Failed to decode the page.");
	}

	return ref new DjvuPage(page, this, pageNumber);
}

IAsyncOperation<DjvuPage^>^ DjvuDocument::GetPageAsync(uint32_t pageNumber)
{
	if (pageNumber < 1 || pageNumber > pageCount)
	{
		throw ref new InvalidArgumentException("pageNumber is out of range");
	}

	return create_async([=]	{
		return GetPage(pageNumber);
	});
}