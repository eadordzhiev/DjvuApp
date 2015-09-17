#pragma once

typedef struct ddjvu_document_s ddjvu_document_t;

namespace DjvuApp { namespace Djvu 
{
	public enum class ZoneType
	{
		Page,
		Column,
		Region,
		Paragraph,
		Line,
		Word,
		Character
	};

	public ref class TextLayerZone sealed
	{
	public:
		property uint32_t StartIndex
		{
			uint32_t get() { return startIndex; }
		}
		property uint32_t EndIndex
		{
			uint32_t get() { return endIndex; }
		}
		property ZoneType Type
		{
			ZoneType get() { return type; }
		}
		property Windows::Foundation::Collections::IVectorView<TextLayerZone^>^ Children
		{
			Windows::Foundation::Collections::IVectorView<TextLayerZone^>^ get() { return children; }
		}
		property Windows::Foundation::Rect Bounds
		{
			Windows::Foundation::Rect get() { return bounds; }
		}
		property Platform::String^ Text
		{
			Platform::String^ get() { return text; }
		}
	internal:
		uint32_t startIndex, endIndex;
		ZoneType type;
		Windows::Foundation::Collections::IVectorView<TextLayerZone^>^ children;
		Windows::Foundation::Rect bounds;
		Platform::String^ text;

		static TextLayerZone^ GetTextLayer(ddjvu_document_s* document, uint32_t pageNumber);
	};
} }