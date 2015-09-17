#include "pch.h"
#include "TextLayer.h"

#include "libdjvu\DjVuImage.h"
#include "libdjvu\DjVuDocument.h"
#include "libdjvu\DjVuText.h"
#include "libdjvu\ddjvuapi.h"
#include "StringConversion.h"

using namespace std;

using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace DjvuApp::Djvu;

static struct zone_match
{
	ZoneType zoneType;
	DjVuTXT::ZoneType ztype;
	char separator;
}
zone_matches[] =
{
	{ ZoneType::Page, DjVuTXT::PAGE, 0 },
	{ ZoneType::Column, DjVuTXT::COLUMN, DjVuTXT::end_of_column },
	{ ZoneType::Region, DjVuTXT::REGION, DjVuTXT::end_of_region },
	{ ZoneType::Paragraph, DjVuTXT::PARAGRAPH, DjVuTXT::end_of_paragraph },
	{ ZoneType::Line, DjVuTXT::LINE, DjVuTXT::end_of_line },
	{ ZoneType::Word, DjVuTXT::WORD, ' ' },
	{ ZoneType::Character, DjVuTXT::CHARACTER, 0 }
};

TextLayerZone^ readZone(const GP<DjVuTXT> &txt, DjVuTXT::Zone &zone, uint32_t& currentIndex)
{
	zone_match zoneMatch;
	for (auto match : zone_matches)
	{
		if (zone.ztype == match.ztype)
		{
			zoneMatch = match;
			break;
		}
	}

	Rect bounds;
	bounds.X = zone.rect.xmin;
	bounds.Y = zone.rect.ymin;
	bounds.Width = zone.rect.width();
	bounds.Height = zone.rect.height();

	auto result = ref new TextLayerZone();
	result->type = zoneMatch.zoneType;
	result->bounds = bounds;
	result->startIndex = currentIndex;

	vector<TextLayerZone^> children;

	if (result->type == ZoneType::Word)
	{
		auto data = (const char*)txt->textUTF8 + zone.text_start;
		auto length = zone.text_length;
		if (length > 0 && data[length - 1] == zoneMatch.separator)
			length -= 1;

		string text(data, length);
		result->text = utf8_to_ps(text);
	}
	else
	{
		for (GPosition pos = zone.children; pos; ++pos)
		{
			currentIndex++;
			auto textLayerZone = readZone(txt, zone.children[pos], currentIndex);
			children.push_back(textLayerZone);
		}
	}

	result->children = ref new VectorView<TextLayerZone^>(children);
	result->endIndex = currentIndex;

	return result;
}

TextLayerZone^ TextLayerZone::GetTextLayer(ddjvu_document_s* document, uint32_t pageNumber)
{
	auto djvuDocument = ddjvu_get_DjVuDocument(document);

	auto file = djvuDocument->get_djvu_file(pageNumber - 1);
	if (!file || !file->is_data_present())
	{
		return nullptr;
	}

	auto bs = file->get_text();
	if (!bs)
	{
		return nullptr;
	}

	auto text = DjVuText::create();
	text->decode(bs);
	auto txt = text->txt;
	if (!txt)
	{
		return nullptr;
	}

	uint32_t index = 0;
	return readZone(txt, txt->page_zone, index);
}