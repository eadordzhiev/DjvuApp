using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
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
        public uint Width { get; private set; }

        [UsedImplicitly]
        public uint Height { get; private set; }

        [UsedImplicitly]
        public ImageSource Source
        {
            get
            {
                if (_source == null)
                {
                    Render();
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

        private static readonly TasksQueue _queue = new TasksQueue();

        private readonly DjvuAsyncDocument _document;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private DjvuAsyncPage _page;

        private ImageSource _source;

        public DjvuPageSource(
            DjvuAsyncDocument document, 
            uint pageNumber, uint width, 
            uint height, 
            double scaleFactor, 
            double previewScaleFactor)
        {
            PageNumber = pageNumber;
            Width = width;
            Height = height;
            _document = document;
        }

        private void Render()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();

            if (_page == null)
            {
                _queue.EnqueueToCurrentThreadAsync(LoadPageAsync, 2, _cts.Token);
            }

            _queue.EnqueueToCurrentThreadAsync(() => RenderAtScaleAsync(1 / 16D), 2, _cts.Token);
            _queue.EnqueueToCurrentThreadAsync(() => RenderAtScaleAsync(1 / 4D), 1, _cts.Token);
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