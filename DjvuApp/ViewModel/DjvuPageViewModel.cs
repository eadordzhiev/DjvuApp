using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Annotations;
using DjvuLibRT;

namespace DjvuApp.ViewModel
{
    //class TasksQueue
    //{
    //    private ConcurrentQueue<Task> tasks = new ConcurrentQueue<Task>(); 

    //    void Enqueue(Task task)
    //    {
    //        AsyncManualResetEvent 
    //        tasks.Enqueue(task);

    //    }
    //}

    public sealed class DjvuPageViewModel : INotifyPropertyChanged 
    {
        public uint PageNumber { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

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
        private DjvuPage _page;
        private ImageSource _source;

        public DjvuPageViewModel(DjvuDocument document, uint pageNumber, PageInfo pageInfo)
        {
            PageNumber = pageNumber;
            _document = document;

            Width = pageInfo.Width;
            Height = pageInfo.Height;
        }
        
        private async void Render()
        {
            Debug.WriteLine("Render(): page {0}", PageNumber);

            cts = new CancellationTokenSource();

            if (_page == null)
            {
                await LoadPageAsync();
            }

            await RenderAtScaleAsync(1 / 16D);
            await RenderAtScaleAsync(1 / 2D);

            _source = null;
        }

        static SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public CancellationTokenSource cts = new CancellationTokenSource();

        private async Task LoadPageAsync()
        {
            Debug.WriteLine("LoadPageAsync({0}) wants to acquire lock", PageNumber);
            try
            {
                await semaphore.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            Debug.WriteLine("LoadPageAsync({0}) has acquired lock", PageNumber);

            var s = Stopwatch.StartNew();

            _page = await _document.GetPageAsync(PageNumber);

            s.Stop();
            Debug.WriteLine("RenderImpl(): GetPage({1}), {0}ms taken", s.ElapsedMilliseconds, PageNumber);

            Debug.WriteLine("LoadPageAsync({0}) is releasing lock", PageNumber);
            semaphore.Release();

            
        }

        private async Task RenderAtScaleAsync(double scale)
        {
            

            var pixelWidth = (int)(Width * scale);
            var pixelHeight = (int)(Height * scale);
            var size = new Size(pixelWidth, pixelHeight);

            Debug.WriteLine("RenderAtScale({0}, scale: {1}) wants to acquire lock", PageNumber, scale);
            try
            {
                await semaphore.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            Debug.WriteLine("RenderAtScale({0}, scale: {1}) has acquired lock", PageNumber, scale);

            var s = Stopwatch.StartNew();

            var source = new WriteableBitmap(pixelWidth, pixelHeight);
            await _page.RenderRegionAsync(source, size, new Rect(new Point(), size));

            s.Stop();
            Debug.WriteLine("RenderImpl(): page {1}, {0} ms taken", s.ElapsedMilliseconds, PageNumber);

            Debug.WriteLine("RenderAtScale({0}, scale: {1}) is releasing lock", PageNumber, scale);
            semaphore.Release();

            

            Source = source;
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