using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Annotations;
using DjvuLibRT;

namespace DjvuApp.ViewModel
{
    public sealed class DjvuPageViewModel
    {
        public uint PageNumber { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public ImageSource Source
        {
            get { return RenderImpl(); }
        }

        private readonly DjvuDocument _document;
        private DjvuPage _page;
        private const double _currentScale = 1/10D;

        public DjvuPageViewModel(DjvuDocument document, uint pageNumber, PageInfo pageInfo)
        {
            PageNumber = pageNumber;
            _document = document;

            Width = pageInfo.Width;
            Height = pageInfo.Height;
        }

        private ImageSource RenderImpl()
        {
            var s = Stopwatch.StartNew();

            if (_page == null)
                _page = _document.GetPage(PageNumber);

            var pixelWidth = (int)(Width * _currentScale);
            var pixelHeight = (int)(Height * _currentScale);
            var size = new Size(pixelWidth, pixelHeight);

            var source = new WriteableBitmap(pixelWidth, pixelHeight);

            _page.RenderRegion(source, size, new Rect(new Point(), size));

            s.Stop();
            Debug.WriteLine("RenderImpl(): page {1}, {0} ms taken", s.ElapsedMilliseconds, PageNumber);

            return source;
        }
    }
}