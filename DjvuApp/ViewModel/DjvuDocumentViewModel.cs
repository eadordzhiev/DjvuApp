using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DjvuLibRT;
using GalaSoft.MvvmLight.Threading;

namespace DjvuApp.ViewModel
{
    public sealed class DjvuDocumentViewModel : ObservableCollection<DjvuPageViewModel>
    {
        private readonly DjvuDocument _document;

        public DjvuDocumentViewModel(DjvuDocument document, Size? size = null)
        {
            _document = document;

            Stopwatch s = Stopwatch.StartNew();
                var pageInfos = _document.GetPageInfos();
                for (uint i = 0; i < _document.PageCount; i++)
                {
                    if (size != null)
                    {
                        pageInfos[i].Width = (uint) size.Value.Width;
                        pageInfos[i].Height = (uint) size.Value.Height;
                    }
                    Add(new DjvuPageViewModel(_document, i + 1, pageInfos[i]));
                }
            s.Stop();
        }
    }

    public sealed class DjvuPageViewModel
    {
        public uint PageNumber { get; set; }
        private readonly DjvuDocument _document;
        public double Width { get; set; }

        public double Height { get; set; }

        public ImageSource Source
        {
            get { return RenderImpl(); }
        }

        private DjvuPage _djvuPage;
        private double _currentScale = 1 / 10D;

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

            if (_djvuPage == null)
                _djvuPage = _document.GetPage(PageNumber);

            var pixelWidth = (int)(Width * _currentScale);
            var pixelHeight = (int)(Height * _currentScale);
            var size = new Size(pixelWidth, pixelHeight);

            var source = new WriteableBitmap(pixelWidth, pixelHeight);

            _djvuPage.RenderRegion(source, size, new Rect(new Point(), size));

            s.Stop();
            Debug.WriteLine("RenderImpl(): page {1}, {0} ms taken", s.ElapsedMilliseconds, PageNumber);

            return source;
        }
    }
}
