using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;
using DjvuLibRT;

namespace DjvuApp.Djvu
{
    public sealed class DjvuAsyncPage
    {
        public uint Width { get { return _page.Width; } }

        public uint Height { get { return _page.Height; } }

        private readonly DjvuPage _page;
        private readonly SemaphoreSlim _semaphore;

        public DjvuAsyncPage(DjvuPage page, SemaphoreSlim semaphore)
        {
            _page = page;
            _semaphore = semaphore;
        }

        public async Task<WriteableBitmap> RenderPageAtScaleAsync(double scale)
        {
            var pixelWidth = (int)(Width * scale);
            var pixelHeight = (int)(Height * scale);
            var size = new Size(pixelWidth, pixelHeight);

            var bitmap = new WriteableBitmap(pixelWidth, pixelHeight);

            await _semaphore.WaitAsync();
            try
            {
                await _page.RenderRegionAsync(bitmap, size, new Rect(new Point(), size));
            }
            finally
            {
                _semaphore.Release();
            }
            
            return bitmap;
        }
    }
}