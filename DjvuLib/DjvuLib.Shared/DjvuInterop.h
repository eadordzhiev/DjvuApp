#pragma once

namespace DjvuApp { namespace Djvu 
{
    [Windows::Foundation::Metadata::WebHostHidden]
	public enum class DocumentType
	{
		OldBundled = DjVuDocument::DOC_TYPE::OLD_BUNDLED,
		OldIndexed = DjVuDocument::DOC_TYPE::OLD_INDEXED,
		Bundled = DjVuDocument::DOC_TYPE::BUNDLED,
		Indirect = DjVuDocument::DOC_TYPE::INDIRECT,
		SinglePage = DjVuDocument::DOC_TYPE::SINGLE_PAGE,
		UnknownType = DjVuDocument::DOC_TYPE::UNKNOWN_TYPE
	};

    [Windows::Foundation::Metadata::WebHostHidden]
	public value struct DjvuBookmark sealed
	{
        Platform::String^ Name;
        Platform::String^ Url;
		unsigned int ChildrenCount;
	};

    [Windows::Foundation::Metadata::WebHostHidden]
	public value struct PageInfo sealed
	{
        uint32_t Height;
        uint32_t Width;
        uint32_t Dpi;
        uint32_t PageNumber;
	};

	ref class DjvuDocument;

	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class DjvuPage sealed
	{
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
        DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint32_t pageNumber);
        void RenderRegion(
            void* bufferPtr,
            Windows::Foundation::Size rescaledPageSize,
            Windows::Foundation::Rect renderRegion
            );
	private:
        uint32_t width, height, pageNumber;
		DjvuDocument^ document;
		ddjvu_page_t* page;
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
		property DocumentType Type
		{
			DocumentType get() { return doctype; }
		}
        static Windows::Foundation::IAsyncOperation<DjvuDocument^>^ LoadAsync(Platform::String^ path);
        [Windows::Foundation::Metadata::DefaultOverloadAttribute]
        static Windows::Foundation::IAsyncOperation<DjvuDocument^>^ LoadAsync(Windows::Storage::IStorageFile^ file);
        Platform::Array<DjvuBookmark>^ GetBookmarks();
        DjvuPage^ GetPage(uint32_t pageNumber);
        Windows::Foundation::IAsyncOperation<DjvuPage^>^ GetPageAsync(uint32_t pageNumber);
        Platform::Array<PageInfo>^ GetPageInfos();
	private:
		ddjvu_context_t* context;
		ddjvu_document_t* document = nullptr;
        uint32_t pageCount = 0;
		DocumentType doctype;
        Platform::Array<PageInfo>^ pageInfos;

		DjvuDocument(const char* path);
	};
} }