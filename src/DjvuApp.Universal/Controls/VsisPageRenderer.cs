using System;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Xaml.Media;
using DjvuApp.Djvu;
using DjvuApp.Misc;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace DjvuApp.Controls
{
    public sealed class VsisPageRenderer : IDisposable
    {
        public ImageSource Source => _vsis?.Source;

        private DjvuPage _page;
        private CanvasVirtualImageSource _vsis;

        public VsisPageRenderer(DjvuPage page, Size pageViewSize)
        {
            _page = page;

            var size = FindBestSize(pageViewSize);

            _vsis = new CanvasVirtualImageSource(
                resourceCreator: CanvasDevice.GetSharedDevice(),
                width: (float) size.Width,
                height: (float) size.Height,
                dpi: DisplayInformation.GetForCurrentView().LogicalDpi,
                alphaMode: CanvasAlphaMode.Ignore);
            _vsis.RegionsInvalidated += RegionsInvalidatedHandler;
        }

        ~VsisPageRenderer()
        {
            Dispose();
        }

        Size FindBestSize(Size desiredSize)
        {
            var rawPixelsPerViewPixel = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var pageWidth = _page.Width / rawPixelsPerViewPixel;
            var pageHeight = _page.Height / rawPixelsPerViewPixel;

            int red;
            for (red = 1; red < 16; red++)
            {
                if (pageWidth / red < desiredSize.Width &&
                    pageHeight / red < desiredSize.Height)
                    break;
            }

            return new Size(pageWidth / red, pageHeight / red);
        }

        private void RegionsInvalidatedHandler(CanvasVirtualImageSource sender, CanvasRegionsInvalidatedEventArgs args)
        {
            foreach (var region in args.InvalidatedRegions)
            {
                RenderRegion(region);
            }
        }

        private uint ConvertDipsToPixels(double dips)
        {
            return (uint) _vsis.ConvertDipsToPixels((float) dips, CanvasDpiRounding.Floor);
        }

        private void RenderRegion(Rect updateRect)
        {
            var region = new BitmapBounds
            {
                X = ConvertDipsToPixels(updateRect.X),
                Y = ConvertDipsToPixels(updateRect.Y),
                Width = ConvertDipsToPixels(updateRect.Width),
                Height = ConvertDipsToPixels(updateRect.Height)
            };

            using (var bitmap = _page.RenderRegionToSoftwareBitmap(_vsis.SizeInPixels, region))
            {
                using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(
                    resourceCreator: CanvasDevice.GetSharedDevice(),
                    sourceBitmap: bitmap))
                using (var drawingSession = _vsis.CreateDrawingSession(Colors.White, updateRect))
                {
                    drawingSession.DrawImage(canvasBitmap, updateRect);
                }
            }
        }

        public void Dispose()
        {
            if (_vsis == null)
                return;

            _vsis.RegionsInvalidated -= RegionsInvalidatedHandler;
            _vsis = null;
            _page = null;

            GC.SuppressFinalize(this);
        }
    }
}