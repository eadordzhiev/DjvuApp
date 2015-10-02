#pragma once

#include "Page.h"
#include "Outline.h"
#include "TextLayer.h"

typedef struct ddjvu_context_s ddjvu_context_t;
typedef struct ddjvu_document_s ddjvu_document_t;

namespace DjvuApp { namespace Djvu 
{
    [Windows::Foundation::Metadata::WebHostHidden]
	public value struct PageInfo sealed
	{
        uint32_t Height;
        uint32_t Width;
        uint32_t Dpi;
	};
	
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class DjvuDocument sealed
	{
	public:
		virtual ~DjvuDocument();
        property uint32_t PageCount
		{
            uint32_t get() { return pageCount; }
		}
        static Windows::Foundation::IAsyncOperation<DjvuDocument^>^ LoadAsync(Windows::Storage::IStorageFile^ file);
		Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^>^ GetOutlineAsync();
		Windows::Foundation::IAsyncOperation<TextLayerZone^>^ GetTextLayerAsync(uint32_t pageNumber);
        Windows::Foundation::IAsyncOperation<DjvuPage^>^ GetPageAsync(uint32_t pageNumber);
        Platform::Array<PageInfo>^ GetPageInfos();
	private:
		ddjvu_context_t* context;
		ddjvu_document_t* document = nullptr;
        uint32_t pageCount = 0;
        Platform::Array<PageInfo>^ pageInfos;

		DjvuDocument(Windows::Storage::Streams::IRandomAccessStream^ stream);
		DjvuPage^ GetPage(uint32_t pageNumber);
	};
} }