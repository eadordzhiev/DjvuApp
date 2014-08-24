using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Misc;
using DjvuLibRT;
using JetBrains.Annotations;

namespace DjvuApp.Controls
{
    public sealed class DjvuPageSource : INotifyPropertyChanged, IDisposable
    {
        [UsedImplicitly]
        public uint PageNumber { get; set; }

        [UsedImplicitly]
        public uint Width { get; set; }

        [UsedImplicitly]
        public uint Height { get; set; }

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

        private readonly DjvuDocument _document;
        private readonly TasksQueue _queue;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private DjvuPage _page;
        private ImageSource _source;

        public DjvuPageSource(DjvuDocument document, PageInfo pageInfo, TasksQueue queue)
        {
            _document = document;
            _queue = queue;
            PageNumber = pageInfo.PageNumber;
            Width = pageInfo.Width;
            Height = pageInfo.Height;
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
            var pixelWidth = (int)(Width * scale);
            var pixelHeight = (int)(Height * scale);
            var size = new Size(pixelWidth, pixelHeight);
            
            var source = new WriteableBitmap(pixelWidth, pixelHeight);
            await _page.RenderRegionAsync(source, size, new Rect(new Point(), size));

            Source = source;
            _source = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}