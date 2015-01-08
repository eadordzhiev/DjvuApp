using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Misc;

namespace DjvuApp.Djvu
{
    public sealed class DjvuAsyncPage
    {
        public uint Width { get { return _page.Width; } }

        public uint Height { get { return _page.Height; } }

        private readonly DjvuPage _page;

        public DjvuAsyncPage(DjvuPage page)
        {
            _page = page;
        }

        public async Task<WriteableBitmap> RenderPageAtScaleAsync(double scale)
        {
            var pixelWidth = (int) (Width * scale);
            var pixelHeight = (int) (Height * scale);
            var size = new Size(pixelWidth, pixelHeight);

            var bitmap = new WriteableBitmap(pixelWidth, pixelHeight);

            //await _page.RenderRegionAsync(bitmap, size, new Rect(new Point(), size));
            var pixelsPointer = IBufferUtilities.GetPointer(bitmap.PixelBuffer);
            _page.RenderRegion(pixelsPointer, size, new Rect(new Point(), size));

            return bitmap;
        }
    }
}