#pragma once

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::UI::Xaml::Media::Imaging;

namespace DjvuLibRT
{
	public enum class DocumentType
	{
		OldBundled = DjVuDocument::DOC_TYPE::OLD_BUNDLED,
		OldIndexed = DjVuDocument::DOC_TYPE::OLD_INDEXED,
		Bundled = DjVuDocument::DOC_TYPE::BUNDLED,
		Indirect = DjVuDocument::DOC_TYPE::INDIRECT,
		SinglePage = DjVuDocument::DOC_TYPE::SINGLE_PAGE,
		UnknownType = DjVuDocument::DOC_TYPE::UNKNOWN_TYPE
	};

	public value struct DjvuBookmark sealed
	{
		String^ Name;
		String^ Url;
		unsigned int ChildrenCount;
	};

	public value struct PageInfo sealed
	{
		unsigned int Height;
		unsigned int Width;
		unsigned int Dpi;
		unsigned int PageNumber;
	};

	ref class DjvuDocument;

	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class DjvuPage sealed
	{
	public:
		virtual ~DjvuPage();
		property unsigned int Width
		{
			unsigned int get() { return width; }
		}
		property unsigned int Height
		{
			unsigned int get() { return height; }
		}
		property unsigned int PageNumber
		{
			unsigned int get() { return pageNumber; }
		}
		IAsyncAction^ RenderRegionAsync(WriteableBitmap^ bitmap, Size rescaledPageSize, Rect renderRegion);
	internal:
		DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, unsigned int pageNumber);
	private:
		unsigned int width, height, pageNumber;
		DjvuDocument^ document;
		ddjvu_page_t* page;
	};

	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class DjvuDocument sealed
	{
	public:
		virtual ~DjvuDocument();
		property unsigned int PageCount
		{
			unsigned int get() { return pageCount; }
		}
		property DocumentType Type
		{
			DocumentType get() { return doctype; }
		}
		static IAsyncOperation<DjvuDocument^>^ LoadAsync(String^ path);
		Array<DjvuBookmark>^ GetBookmarks();
		IAsyncOperation<DjvuPage^>^ GetPageAsync(unsigned int pageNumber);
		Array<PageInfo>^ GetPageInfos();
	private:
		ddjvu_context_t* context;
		ddjvu_format_t* format;
		ddjvu_document_t* document = nullptr;
		unsigned int pageCount = 0;
		DocumentType doctype;
		Array<PageInfo>^ pageInfos;

		DjvuDocument(const char* path);
	internal:
		ddjvu_format_t* GetFormat();
	};
}