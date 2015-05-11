#pragma once
#include "DjvuInterop.h"
#include "Renderer.h"

namespace DjvuApp
{
    [Windows::Foundation::Metadata::WebHostHidden]
    public ref class VsisWrapper sealed
    {
    public:
        VsisWrapper(Djvu::DjvuPage^ page, Renderer^ renderer, Windows::Foundation::Size pageViewSize);
        virtual ~VsisWrapper();

        property Windows::UI::Xaml::Media::Imaging::VirtualSurfaceImageSource^ Source
        {
            Windows::UI::Xaml::Media::Imaging::VirtualSurfaceImageSource^ get()
            {
                return vsis;
            }
        }

        void CreateSurface();

    internal:
        void UpdatesNeeded();

    private:
        void RenderRegion(const RECT& updateRect);
        void HandleDeviceLost();

    private:
        uint32 width, height;
        Djvu::DjvuPage^ page;
        Windows::UI::Xaml::Media::Imaging::VirtualSurfaceImageSource^ vsis;
        Microsoft::WRL::ComPtr<IVirtualSurfaceImageSourceNative> vsisNative;
        Renderer^ renderer;
    };
}