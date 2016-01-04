#include "pch.h"
#include "Page.h"

#include "libdjvu\DjVuImage.h"
#include "libdjvu\ddjvuapi.h"
#include "DebugHelper.h"
#include "IBufferUtilities.h"

using namespace concurrency;
using namespace std;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Graphics::Imaging;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace DjvuApp::Djvu;
using namespace DjvuApp::Misc;

DjvuPage::DjvuPage(ddjvu_page_t* page, DjvuDocument^ document, uint32_t pageNumber)
{
	this->page = page;
	this->document = document;
	this->width = ddjvu_page_get_width(page);
	this->height = ddjvu_page_get_height(page);
	this->pageNumber = pageNumber;
}

DjvuPage::~DjvuPage()
{
	document = nullptr;

	if (page != nullptr)
	{
		ddjvu_page_release(page);
		page = nullptr;
	}
}

void DjvuPage::RenderRegion(void* bufferPtr, BitmapSize rescaledPageSize, BitmapBounds renderRegion)
{
	if (page == nullptr)
	{
		throw ref new ObjectDisposedException();
	}
	
	ddjvu_rect_t prect;
	ddjvu_rect_t rrect;
	size_t rowsize;

	rrect.x = renderRegion.X;
	rrect.y = renderRegion.Y;
	rrect.w = renderRegion.Width;
	rrect.h = renderRegion.Height;

	prect.x = 0;
	prect.y = 0;
	prect.w = rescaledPageSize.Width;
	prect.h = rescaledPageSize.Height;

	rowsize = rrect.w * 4;

	auto format = ddjvu_format_create(ddjvu_format_style_t::DDJVU_FORMAT_BGRA, 0, nullptr);
	ddjvu_format_set_row_order(format, 1);
	ddjvu_format_set_y_direction(format, 1);

	if (!ddjvu_page_render(page, DDJVU_RENDER_COLOR, &prect, &rrect, format, rowsize, (char*)bufferPtr))
	{
		DBGPRINT(L"Cannot render page, no data.");
		memset(bufferPtr, UINT_MAX, rowsize * rrect.h);
	}

	ddjvu_format_release(format);
}

IAsyncAction^ DjvuPage::RenderRegionAsync(WriteableBitmap^ bitmap, BitmapSize rescaledPageSize, BitmapBounds renderRegion)
{
	if (bitmap == nullptr)
	{
		throw ref new NullReferenceException("Bitmap is null.");
	}

	auto pixelBuffer = bitmap->PixelBuffer;
	
	if (pixelBuffer->Length < renderRegion.Width * renderRegion.Height * 4)
	{
		throw ref new InvalidArgumentException("Buffer is too small.");
	}

	auto bufferPtr = IBufferUtilities::GetPointer(pixelBuffer);

	if (bufferPtr == nullptr)
	{
		throw ref new NullReferenceException("bufferPtr == nullptr");
	}

	return create_async([=]
	{
		RenderRegion(bufferPtr, rescaledPageSize, renderRegion);
	});
}

SoftwareBitmap^ DjvuPage::RenderRegionToSoftwareBitmap(BitmapSize rescaledPageSize, BitmapBounds renderRegion)
{
	auto softwareBitmap = ref new SoftwareBitmap(BitmapPixelFormat::Bgra8, renderRegion.Width, renderRegion.Height, BitmapAlphaMode::Ignore);
	auto bitmapBuffer = softwareBitmap->LockBuffer(BitmapBufferAccessMode::Write);
	auto memoryBufferReference = bitmapBuffer->CreateReference();

	void* pointer;
	UINT32 capacity;
	IBufferUtilities::GetPointer(memoryBufferReference, &pointer, &capacity);

	RenderRegion(pointer, rescaledPageSize, renderRegion);

	delete memoryBufferReference;
	delete bitmapBuffer;

	return softwareBitmap;
}