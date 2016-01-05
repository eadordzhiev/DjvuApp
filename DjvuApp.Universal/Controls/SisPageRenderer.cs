using System;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI;
using DjvuApp.Djvu;
using DjvuApp.Misc;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace DjvuApp.Controls
{
    public sealed class SisPageRenderer
    {
        public CanvasImageSource Source { get; private set; }

        private DjvuPage _page;

        public SisPageRenderer(DjvuPage page, Size pageViewSize)
        {
            _page = page;
            var rawPixelsPerViewPixel = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var width = Math.Min(pageViewSize.Width, page.Width / rawPixelsPerViewPixel);
            var height = Math.Min(pageViewSize.Height, page.Height / rawPixelsPerViewPixel);

            Source = new CanvasImageSource(
                resourceCreator: CanvasDevice.GetSharedDevice(),
                width: (float)width,
                height: (float)height,
                dpi: DisplayInformation.GetForCurrentView().LogicalDpi,
                alphaMode: CanvasAlphaMode.Ignore);

            RenderRegion(new Rect(0, 0, width, height));

            _page = null;
        }
        
        private uint ConvertDipsToPixels(double dips)
        {
            return (uint)Source.ConvertDipsToPixels((float)dips, CanvasDpiRounding.Floor);
        }

        private void RenderRegion(Rect updateRect)
        {
            var renderRegion = new BitmapBounds
            {
                X = ConvertDipsToPixels(updateRect.X),
                Y = ConvertDipsToPixels(updateRect.Y),
                Width = ConvertDipsToPixels(updateRect.Width),
                Height = ConvertDipsToPixels(updateRect.Height)
            };

            using (var bitmap = _page.RenderRegionToSoftwareBitmap(Source.SizeInPixels, renderRegion))
            {
                using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(
                    resourceCreator: CanvasDevice.GetSharedDevice(),
                    sourceBitmap: bitmap))
                using (var drawingSession = Source.CreateDrawingSession(Colors.White, updateRect))
                {
                    drawingSession.DrawImage(canvasBitmap, updateRect);
                }
            }
        }
    }
}