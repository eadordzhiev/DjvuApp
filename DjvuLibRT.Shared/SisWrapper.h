#pragma once
#include "DjvuInterop.h"
#include "Renderer.h"

namespace DjvuApp
{
    [Windows::Foundation::Metadata::WebHostHidden]
    public ref class SisWrapper sealed
    {
    public:
        SisWrapper(Djvu::DjvuPage^ page, Renderer^ renderer, Windows::Foundation::Size pageViewSize);
        virtual ~SisWrapper();

        property Windows::UI::Xaml::Media::Imaging::SurfaceImageSource^ Source
        {
            Windows::UI::Xaml::Media::Imaging::SurfaceImageSource^ get()
            {
                return vsis;
            }
        }

        void CreateSurface();

    private:
        void RenderRegion(const RECT& updateRect);
        void HandleDeviceLost();

    private:
        uint32 width, height;
        Djvu::DjvuPage^ page;
        Windows::UI::Xaml::Media::Imaging::SurfaceImageSource^ vsis;
        Microsoft::WRL::ComPtr<ISurfaceImageSourceNative> vsisNative;
        Renderer^ renderer;
    };
}