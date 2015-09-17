#include "pch.h"
#include "Outline.h"

#include "libdjvu\DjVuImage.h"
#include "libdjvu\DjVuDocument.h"
#include "libdjvu\DjVmNav.h"
#include "libdjvu\ddjvuapi.h"
#include "StringConversion.h"

using namespace std;

using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace DjvuApp::Djvu;

DjvuOutlineItem::DjvuOutlineItem(String^ name, uint32_t pageNumber, IVectorView<DjvuOutlineItem^>^ items) :
	name(name),
	pageNumber(pageNumber),
	items(items)
{ }

IVectorView<DjvuOutlineItem^>^ processNavChunk(const GP<DjVmNav> &nav, int &pos, int count, ddjvu_document_t* document)
{
	vector<DjvuOutlineItem^> result;

	for (; count > 0 && pos < nav->getBookMarkCount(); count--)
	{
		GP<DjVmNav::DjVuBookMark> entry;
		nav->getBookMark(entry, pos++);

		auto name = utf8_to_ps((const char*)entry->displayname);
		auto url = (const char*)entry->url;
		auto items = processNavChunk(nav, pos, entry->count, document);

		uint32_t pageNumber = 0;
		if (url[0] == '#' && url[1] != '\0')
		{
			pageNumber = ddjvu_document_search_pageno(document, &url[1]) + 1;
		}

		auto item = ref new DjvuOutlineItem(name, pageNumber, items);
		result.push_back(item);
	}

	return ref new VectorView<DjvuOutlineItem^>(move(result));
}

IVectorView<DjvuOutlineItem^>^ DjvuOutlineItem::GetOutline(ddjvu_document_s* document)
{
	auto djvuDocument = ddjvu_get_DjVuDocument(document);
	auto nav = djvuDocument->get_djvm_nav();

	if (!nav)
	{
		return nullptr;
	}

	int pos = 0;
	auto result = processNavChunk(nav, pos, nav->getBookMarkCount(), document);

	return result;
}