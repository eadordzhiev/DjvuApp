#pragma once

typedef struct ddjvu_document_s ddjvu_document_t;

namespace DjvuApp { namespace Djvu 
{	
    [Windows::Foundation::Metadata::WebHostHidden]
	public ref class DjvuOutlineItem sealed
	{
	public:
		DjvuOutlineItem(Platform::String^ name, uint32_t pageNumber, Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ items);
		property Platform::String^ Name
		{
			Platform::String^ get() { return name; }
		}
		property uint32_t PageNumber
		{
			uint32_t get() { return pageNumber; }
		}
		property Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ Items
		{
			Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ get() { return items; }
		}
	internal:
		static Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ GetOutline(ddjvu_document_s* document);
	private:
        Platform::String^ name;
		uint32_t pageNumber;
		Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ items;
	};
} }