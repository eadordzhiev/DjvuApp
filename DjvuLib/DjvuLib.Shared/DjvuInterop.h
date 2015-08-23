#pragma once

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
	};
	
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
	private:
        Platform::String^ name;
		uint32_t pageNumber;
		Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ items;
	};

    [Windows::Foundation::Metadata::WebHostHidden]
	public value struct PageInfo sealed
	{
        uint32_t Height;
        uint32_t Width;
        uint32_t Dpi;
	};

	ref class DjvuDocument;

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
			Windows::Foundation::Size rescaledPageSize,
			Windows::Foundation::Rect renderRegion
			);
	internal:
        void RenderRegion(
            void* bufferPtr,
            Windows::Foundation::Size rescaledPageSize,
            Windows::Foundation::Rect renderRegion
            );
	private:
        uint32_t width, height, pageNumber;
		DjvuDocument^ document;
		ddjvu_page_t* page;

		DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint32_t pageNumber);
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
		Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ ProcessOutlineExpression(miniexp_t current);
	};
} }