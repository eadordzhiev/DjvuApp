#pragma once

typedef struct ddjvu_page_s ddjvu_page_t;

namespace DjvuApp { namespace Djvu 
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class DjvuPage sealed
	{
		friend ref class DjvuDocument;
	public:
		virtual ~DjvuPage();
        property uint32_t Width
		{
            uint32_t get() { return width; }
		}
        property uint32_t Height
		{
            uint32_t get() { return height; }
		}
        property uint32_t PageNumber
		{
            uint32_t get() { return pageNumber; }
		}
		Windows::Foundation::IAsyncAction^ RenderRegionAsync(
			Windows::UI::Xaml::Media::Imaging::WriteableBitmap^ bitmap,
			Windows::Graphics::Imaging::BitmapSize rescaledPageSize,
			Windows::Graphics::Imaging::BitmapBounds renderRegion
			);
		Windows::Graphics::Imaging::SoftwareBitmap^ RenderRegionToSoftwareBitmap(
			Windows::Graphics::Imaging::BitmapSize rescaledPageSize,
			Windows::Graphics::Imaging::BitmapBounds renderRegion
			);
	internal:
        void RenderRegion(
            void* bufferPtr,
			Windows::Graphics::Imaging::BitmapSize rescaledPageSize,
			Windows::Graphics::Imaging::BitmapBounds renderRegion
            );
	private:
        uint32_t width, height, pageNumber;
		DjvuDocument^ document;
		ddjvu_page_t* page;

		DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint32_t pageNumber);
	};
} }