#pragma once

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
        static Windows::Foundation::IAsyncOperation<DjvuDocument^>^ LoadAsync(Platform::String^ path);
        [Windows::Foundation::Metadata::DefaultOverloadAttribute]
        static Windows::Foundation::IAsyncOperation<DjvuDocument^>^ LoadAsync(Windows::Storage::IStorageFile^ file);
		Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ GetOutline();
        DjvuPage^ GetPage(uint32_t pageNumber);
        Windows::Foundation::IAsyncOperation<DjvuPage^>^ GetPageAsync(uint32_t pageNumber);
        Platform::Array<PageInfo>^ GetPageInfos();
	private:
		ddjvu_context_t* context;
		ddjvu_document_t* document = nullptr;
        uint32_t pageCount = 0;
        Platform::Array<PageInfo>^ pageInfos;

		DjvuDocument(const char* path);
		Windows::Foundation::Collections::IVectorView<DjvuOutlineItem^>^ ProcessOutlineExpression(miniexp_t current);
	};
} }