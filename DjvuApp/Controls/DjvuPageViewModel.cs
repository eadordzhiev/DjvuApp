using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Djvu;
using DjvuApp.Misc;
using JetBrains.Annotations;

namespace DjvuApp.Controls
{
    public sealed class DjvuPageSource : INotifyPropertyChanged, IDisposable
    {
        public static event EventHandler PageRendered;

        [UsedImplicitly]
        public uint PageNumber { get; private set; }

        [UsedImplicitly]
        public float Width { get; private set; }

        [UsedImplicitly]
        public float Height { get; private set; }

        [UsedImplicitly]
        public ImageSource Source
        {
            get
            {
                if (_source == null)
                {
                    Render();
                    return _placeholderBitmap;
                }
                return _source;
            }
            set
            {
                if (_source != value)
                {
                    _source = value;
                    RaisePropertyChanged();
                }
            }
        }

        private static readonly ImageSource _placeholderBitmap;
        private static readonly double _displayScaleFactor;
        private static readonly TasksQueue _queue = new TasksQueue();

        private readonly DjvuAsyncDocument _document;
        private readonly double _scaleFactor;
        private readonly double _previewScaleFactor;

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private DjvuAsyncPage _page;
        private ImageSource _source;

        static DjvuPageSource()
        {
            _placeholderBitmap = new BitmapImage(new Uri("ms-appx:///Assets/PlaceholderImage.png"));
            _displayScaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
        }

        public DjvuPageSource(
            DjvuAsyncDocument document, 
            uint pageNumber, uint width, 
            uint height, 
            double scaleFactor, 
            double previewScaleFactor)
        {
            PageNumber = pageNumber;
            Width = (float) (width / _displayScaleFactor);
            Height = (float) (height / _displayScaleFactor);
            _document = document;
            _scaleFactor = scaleFactor;
            _previewScaleFactor = previewScaleFactor;
        }

        private void Render()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();

            if (_page == null)
            {
                _queue.EnqueueToCurrentThreadAsync(LoadPageAsync, 2, _cts.Token);
            }

            _queue.EnqueueToCurrentThreadAsync(() => RenderAtScaleAsync(_previewScaleFactor), 2, _cts.Token);
            _queue.EnqueueToCurrentThreadAsync(() => RenderAtScaleAsync(_scaleFactor), 1, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        private async Task LoadPageAsync()
        {
            _page = await _document.GetPageAsync(PageNumber);
        }

        private async Task RenderAtScaleAsync(double scale)
        {
            var bitmap = await _page.RenderPageAtScaleAsync(scale);
            Source = bitmap;
            _source = null;

            OnPageRendered();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void OnPageRendered()
        {
            var handler = PageRendered;
            if (handler != null) handler(null, EventArgs.Empty);
        }
    }
}