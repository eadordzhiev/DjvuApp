#include "pch.h"
#include "SisWrapper.h"

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Graphics::Display;
using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Microsoft::WRL;
using namespace DjvuApp::Djvu;

namespace DjvuApp
{
    SisWrapper::SisWrapper(DjvuPage^ page, Renderer^ renderer, Size pageViewSize) :
        page(page),
        renderer(renderer),
        vsisNative(nullptr)
    {
        double dpiFactor = DisplayInformation::GetForCurrentView()->RawPixelsPerViewPixel;
        width = static_cast<uint32>(pageViewSize.Width * dpiFactor);
        height = static_cast<uint32>(pageViewSize.Height * dpiFactor);
    }

    SisWrapper::~SisWrapper()
    {
        vsis = nullptr;
        vsisNative = nullptr;
        page = nullptr;
        renderer = nullptr;
    }

    void SisWrapper::CreateSurface()
    {
        vsis = ref new SurfaceImageSource(width, height, true);

        DX::ThrowIfFailed(
            reinterpret_cast<IInspectable*>(vsis)->QueryInterface(IID_PPV_ARGS(&vsisNative))
            );

        IDXGIDevice* dxgiDevice;
        renderer->GetDXGIDevice(&dxgiDevice);

        DX::ThrowIfFailed(
            vsisNative->SetDevice(dxgiDevice)
            );
        
        RECT rect = { 0 };
        rect.right = width;
        rect.bottom = height;
        RenderRegion(rect);
    }

    void SisWrapper::RenderRegion(const RECT& updateRect)
    {
        UINT regionWidth = updateRect.right - updateRect.left;
        UINT regionHeight = updateRect.bottom - updateRect.top;
        UINT rowSize = regionWidth * 4;

        Rect renderRegion;
        renderRegion.Width = static_cast<float>(regionWidth);
        renderRegion.Height = static_cast<float>(regionHeight);
        renderRegion.X = static_cast<float>(updateRect.left);
        renderRegion.Y = static_cast<float>(height - updateRect.bottom);
        Size pageSize(width, height);

        void* buffer = new char[regionHeight * rowSize];

        page->RenderRegion(buffer, pageSize, renderRegion);

        auto d2dDeviceContext = renderer->GetD2DDeviceContext();

        ComPtr<ID2D1Bitmap> bitmap;
        DX::ThrowIfFailed(
            d2dDeviceContext->CreateBitmap(
            D2D1::SizeU(regionWidth, regionHeight),
            buffer,
            rowSize,
            D2D1::BitmapProperties(
            D2D1::PixelFormat(
            DXGI_FORMAT_B8G8R8A8_UNORM,
            D2D1_ALPHA_MODE_IGNORE
            )
            ),
            &bitmap)
            );

        delete[] buffer;

        ComPtr<IDXGISurface> dxgiSurface;
        POINT surfaceOffset = { 0 };

        HRESULT hr = vsisNative->BeginDraw(updateRect, &dxgiSurface, &surfaceOffset);

        if (SUCCEEDED(hr))
        {
            ComPtr<ID2D1Bitmap1> targetBitmap;
            DX::ThrowIfFailed(
                d2dDeviceContext->CreateBitmapFromDxgiSurface(
                dxgiSurface.Get(),
                nullptr,
                &targetBitmap
                )
                );
            d2dDeviceContext->SetTarget(targetBitmap.Get());

            auto transform = D2D1::Matrix3x2F::Translation(
                static_cast<float>(surfaceOffset.x),
                static_cast<float>(surfaceOffset.y)
                );
            d2dDeviceContext->SetTransform(transform);

            d2dDeviceContext->BeginDraw();
            d2dDeviceContext->DrawBitmap(bitmap.Get());
            DX::ThrowIfFailed(
                d2dDeviceContext->EndDraw()
                );

            d2dDeviceContext->SetTarget(nullptr);

            DX::ThrowIfFailed(
                vsisNative->EndDraw()
                );
        }
        else if ((hr == DXGI_ERROR_DEVICE_REMOVED) || (hr == DXGI_ERROR_DEVICE_RESET))
        {
            renderer->HandleDeviceLost();
        }
        else
        {
            DX::ThrowIfFailed(hr);
        }
    }
}