using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public class PageLoadScheduler
    {
        class Obj
        {
            public PageViewControlState State { get; set; }
            public Action<DjvuPage, TextLayerZone, CancellationToken> Callback { get; set; }
            public CancellationToken CancellationToken { get; set; }
        }

        public static PageLoadScheduler Instance = new PageLoadScheduler();

        private readonly Queue<Obj> _tasks = new Queue<Obj>();

        private readonly Timer _timer;
        
        private uint _concurrentTasksCount;

        public PageLoadScheduler()
        {
            _timer = new Timer(Timer_Tick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(30));
        }

        public void Subscribe(PageViewControlState state, Action<DjvuPage, TextLayerZone, CancellationToken> callback, CancellationToken ct)
        {
            lock(_tasks)
            {
                _tasks.Enqueue(new Obj { State = state, Callback = callback, CancellationToken = ct });
            }
        }

        private void Start()
        {
            _concurrentTasksCount--;

            if (_concurrentTasksCount < Environment.ProcessorCount)
            {
                _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(30));
            }
        }

        private void Stop()
        {
            _concurrentTasksCount++;

            if (_concurrentTasksCount >= Environment.ProcessorCount)
            {
                _timer.Change(-1, -1);
            }
        }

        private void Timer_Tick(object sender)
        {
            for (int i = 0; i < Environment.ProcessorCount - _concurrentTasksCount; i++)
            {
                Obj task;

                lock (_tasks)
                {
                    if (!_tasks.Any())
                        return;

                    task = _tasks.Dequeue();
                }

                DoJob(task);
            }
        }

        private async void DoJob(Obj obj)
        {
            if (obj.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            Stop();
            var document = obj.State.Document;
            var pageNumber = obj.State.PageNumber;
            var page = await document.GetPageAsync(pageNumber);

            if (obj.CancellationToken.IsCancellationRequested)
            {
                Start();
                return;
            }
            var textLayer = await document.GetTextLayerAsync(pageNumber);
            Start();

            if (obj.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => obj.Callback(page, textLayer, obj.CancellationToken));
        }
    }
}
