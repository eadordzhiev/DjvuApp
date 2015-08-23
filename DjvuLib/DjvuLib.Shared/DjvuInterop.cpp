#include "pch.h"
#include "DjvuInterop.h"
#include "IBufferUtilities.h"
#include "WinrtByteStream.h"

using namespace concurrency;
using namespace std;

using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace DjvuApp;
using namespace DjvuApp::Djvu;
using namespace DjvuApp::Misc;

DjvuOutlineItem::DjvuOutlineItem(String^ name, uint32_t pageNumber, IVectorView<DjvuOutlineItem^>^ items) :
	name(name),
	pageNumber(pageNumber),
	items(items)
{ }

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

IVectorView<DjvuOutlineItem^>^ DjvuDocument::ProcessOutlineExpression(miniexp_t current)
{
	vector<DjvuOutlineItem^> result;

	while (current != miniexp_nil)
	{
		auto itemExp = miniexp_car(current);

		auto name = miniexp_to_str(miniexp_car(itemExp));
		itemExp = miniexp_cdr(itemExp);
		auto url = miniexp_to_str(miniexp_car(itemExp));
		itemExp = miniexp_cdr(itemExp);
		auto items = ProcessOutlineExpression(itemExp);

		uint32_t pageNumber = 0;
		if (url[0] == '#' && url[1] != '\0')
		{
			pageNumber = ddjvu_document_search_pageno(document, &url[1]) + 1;
		}

		auto item = ref new DjvuOutlineItem(utf8_to_ps(name), pageNumber, items);
		
		result.push_back(item);

		current = miniexp_cdr(current);
	}

	return ref new VectorView<DjvuOutlineItem^>(move(result));
}

IAsyncOperation<IVectorView<DjvuOutlineItem^>^>^ DjvuDocument::GetOutlineAsync()
{
	return create_async([=]() -> IVectorView<DjvuOutlineItem^>^
	{
		auto outline = ddjvu_document_get_outline(document);

		if (outline == miniexp_nil)
		{
			return nullptr;
		}

		if (!miniexp_consp(outline) || miniexp_car(outline) != miniexp_symbol("bookmarks"))
		{
			throw ref new FailureException("Outline data is corrupted.");
		}

		outline = miniexp_cdr(outline);

		return ProcessOutlineExpression(outline);
	});
}

miniexp_t readRect(miniexp_t current, Rect& rect)
{
	rect.X = miniexp_to_int(miniexp_car(current));
	current = miniexp_cdr(current);
	rect.Y = miniexp_to_int(miniexp_car(current));
	current = miniexp_cdr(current);
	rect.Width = miniexp_to_int(miniexp_car(current)) - rect.X;
	current = miniexp_cdr(current);
	rect.Height = miniexp_to_int(miniexp_car(current)) - rect.Y;
	current = miniexp_cdr(current);

	return current;
}

struct zone_tag
{
	const char* tag;
	const ZoneType type;
};

static const zone_tag knownTags[] =
{
	{ "page", ZoneType::Page },
	{ "column", ZoneType::Column },
	{ "region", ZoneType::Region },
	{ "para", ZoneType::Paragraph },
	{ "line", ZoneType::Line },
	{ "word", ZoneType::Word },
	{ "char", ZoneType::Character }
};

TextLayerZone^ readZone(miniexp_t current)
{
	if (!miniexp_symbolp(miniexp_car(current)))
	{
		throw ref new FailureException();
	}

	auto result = ref new TextLayerZone();
	auto zoneTypeTag = miniexp_to_name(miniexp_car(current));
	current = miniexp_cdr(current);

	for (auto knownTag : knownTags)
	{
		if (strcmp(zoneTypeTag, knownTag.tag) == 0)
		{
			result->type = knownTag.type;
			break;
		}
	}
		
	current = readRect(current, result->bounds);

	if (miniexp_stringp(miniexp_car(current)))
	{
		auto text = miniexp_to_str(miniexp_car(current));
		result->text = utf8_to_ps(text);
		current = miniexp_cdr(current);
	}

	vector<TextLayerZone^> children;
	while (current != miniexp_nil)
	{
		auto zone = readZone(miniexp_car(current));
		children.push_back(zone);

		current = miniexp_cdr(current);
	}
	result->children = ref new VectorView<TextLayerZone^>(children);

	return result;
}

IAsyncOperation<TextLayerZone^>^ DjvuDocument::GetTextLayerAsync(uint32_t pageNumber)
{
	return create_async([=]() -> TextLayerZone^
	{
		auto current = ddjvu_document_get_pagetext(document, pageNumber - 1, nullptr);

		if (current == miniexp_nil)
		{
			return nullptr;
		}

		return readZone(current);
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

DjvuPage::DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint32_t pageNumber)
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

	if (!ddjvu_page_render(page, DDJVU_RENDER_COLOR, &prect, &rrect, format, rowsize, (char*)bufferPtr))
	{
		DBGPRINT(L"Cannot render page, no data.");
		memset(bufferPtr, UINT_MAX, rowsize * rrect.h);
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