using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public class PageLoadObserver
    {
        class Obj
        {
            public PageViewControlState State { get; set; }
            public Action<DjvuPage> Callback { get; set; }
        }

        public static PageLoadObserver Instance = new PageLoadObserver();

        private Dictionary<int, Obj> states = new Dictionary<int, Obj>();

        readonly DispatcherTimer _timer = new DispatcherTimer();

        private int _lastId;

        public PageLoadObserver()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        public int Subscribe(PageViewControlState state, Action<DjvuPage> callback)
        {
            _lastId++;
            states[_lastId] = new Obj { State = state, Callback = callback };
            return _lastId;
        }
        
        public void Unsubscribe(int id)
        {
            states.Remove(id);
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private async void Timer_Tick(object sender, object e)
        {
            if (!states.Any())
            {
                return;
            }

            var pair = states.Last();
            var obj = pair.Value;
            var id = pair.Key;

            Stop();
            var page = await obj.State.Document.GetPageAsync(obj.State.PageNumber);
            Start();

            if (states.ContainsKey(id))
            {
                states.Remove(id);
                obj.Callback(page);
            }
        }
    }
}
