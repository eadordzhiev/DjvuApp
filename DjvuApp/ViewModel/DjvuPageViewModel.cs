using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using JetBrains.Annotations;
using DjvuLibRT;

namespace DjvuApp.ViewModel
{
    public sealed class DjvuPageViewModel : INotifyPropertyChanged, IDisposable
    {
        class TaskToken
        {
            public CancellationToken CancellationToken { get; private set; }

            public int Priority { get; private set; }

            private readonly Func<Task> _function;

            public TaskToken(Func<Task> function, int priority, CancellationToken cancellationToken)
            {
                _function = function;
                Priority = priority;
                CancellationToken = cancellationToken;
            }

            public async Task ExecuteAsync()
            {
                await _function();
            }
        }

        class TasksQueue
        {
            private readonly List<TaskToken> _list = new List<TaskToken>();
            private bool _isRunning = false;

            private async Task LoopAsync()
            {
                _isRunning = true;

                while (true)
                {
                    TaskToken task;
                    lock (_list)
                    {
                        _list.RemoveAll(item => item.CancellationToken.IsCancellationRequested);

                        if (_list.Count == 0)
                        {
                            _isRunning = false;
                            return;
                        }

                        var maxPriority = _list.Max(item => item.Priority);
                        task = _list.First(item => item.Priority == maxPriority);
                    }

                    await task.ExecuteAsync();

                    lock (_list)
                    {
                        _list.Remove(task);
                    }
                }
            }

            public async void Enqueue(TaskToken token)
            {
                lock (_list)
                {
                    _list.Add(token);
                }

                if (!_isRunning)
                {
                    await LoopAsync();
                }
            }
        }

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

            if (queue == null)
            {
                queue = new TasksQueue();
            }
        }

        private static TasksQueue queue;

        private void Render()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();

            if (_page == null)
            {
                queue.Enqueue(new TaskToken(LoadPageAsync, 2, _cts.Token));
            }

            queue.Enqueue(new TaskToken(() => RenderAtScaleAsync(1 / 16D), 2, _cts.Token));
            queue.Enqueue(new TaskToken(() => RenderAtScaleAsync(1 / 4D), 1, _cts.Token));
        }

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public void Dispose()
        {
            _cts.Cancel();
        }

        private async Task LoadPageAsync()
        {
            var s = Stopwatch.StartNew();

            _page = await _document.GetPageAsync(PageNumber);

            s.Stop();
            Debug.WriteLine("RenderImpl(): GetPage({1}), {0}ms taken", s.ElapsedMilliseconds, PageNumber);
        }

        private async Task RenderAtScaleAsync(double scale)
        {
            var pixelWidth = (int)(Width * scale);
            var pixelHeight = (int)(Height * scale);
            var size = new Size(pixelWidth, pixelHeight);

            var s = Stopwatch.StartNew();

            var source = new WriteableBitmap(pixelWidth, pixelHeight);
            await _page.RenderRegionAsync(source, size, new Rect(new Point(), size));

            s.Stop();
            Debug.WriteLine("RenderImpl(): page {1}, {0} ms taken", s.ElapsedMilliseconds, PageNumber);

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