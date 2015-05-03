#pragma once
#include <initguid.h>
#include <d3d11_2.h>
#include <dxgi1_3.h>
#include <d2d1_2.h>
#include <dwrite_2.h>
#include <wincodec.h>
#include <wrl.h>

namespace DjvuApp
{
    public ref class Renderer sealed
    {
    public:
        Renderer();
        void HandleDeviceLost();
        void SetDpi(_In_ float dpi);
        void Trim();

    internal:
        void GetDXGIDevice(_Outptr_ IDXGIDevice** dxgiDevice);
        Microsoft::WRL::ComPtr<ID2D1DeviceContext> GetD2DDeviceContext();

    protected private:
        void CreateDeviceIndependentResources();
        void CreateDeviceResources();

        // Declare Direct2D objects
        Microsoft::WRL::ComPtr<ID2D1Factory1> d2dFactory;
        Microsoft::WRL::ComPtr<ID2D1Device> d2dDevice;
        Microsoft::WRL::ComPtr<ID2D1DeviceContext> d2dDeviceContext;
        // Direct3D objects
        Microsoft::WRL::ComPtr<IDXGIDevice> dxgiDevice;
        // Direct3D feature level
        D3D_FEATURE_LEVEL featureLevel;
        float dpi;
    };
}

// Helper utilities to make DX APIs work with exceptions in the samples apps.
namespace DX
{
    inline void ThrowIfFailed(_In_ HRESULT hr)
    {
        if (FAILED(hr))
        {
            // Set a breakpoint on this line to catch DX API errors.
            throw Platform::Exception::CreateException(hr);
        }
    }
}