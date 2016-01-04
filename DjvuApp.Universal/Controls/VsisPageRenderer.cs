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
    public class VsisPageRenderer : IDisposable
    {
        public ImageSource Source => _vsis?.Source;

        private DjvuPage _page;
        private CanvasVirtualImageSource _vsis;

        public VsisPageRenderer(DjvuPage page, Size pageViewSize)
        {
            _page = page;

            var rawPixelsPerViewPixel = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var width = (uint) Math.Min(pageViewSize.Width, page.Width / rawPixelsPerViewPixel);
            var height = (uint) Math.Min(pageViewSize.Height, page.Height / rawPixelsPerViewPixel);

            _vsis = new CanvasVirtualImageSource(
                resourceCreator: CanvasDevice.GetSharedDevice(),
                width: width,
                height: height,
                dpi: DisplayInformation.GetForCurrentView().LogicalDpi,
                alphaMode: CanvasAlphaMode.Ignore);
            _vsis.RegionsInvalidated += RegionsInvalidatedHandler;
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
        }
    }
}