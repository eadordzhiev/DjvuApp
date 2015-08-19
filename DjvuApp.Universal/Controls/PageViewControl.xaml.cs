using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed partial class PageViewControl : UserControl
    {
        private static readonly Lazy<Renderer> _renderer = new Lazy<Renderer>(() => new Renderer());

        public PageViewControlState State
        {
            get { return (PageViewControlState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(PageViewControlState), typeof(PageViewControl), new PropertyMetadata(null, StateChangedCallback));

        private VsisWrapper _contentVsis;
        private SisWrapper _thumbnailSis;
        private DjvuPage _page;
        private IZoomFactorObserver _zoomFactorObserver;
        static PageLoadObserver observer = new PageLoadObserver();

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl) d;
            sender.OnStateChanged((PageViewControlState) e.OldValue, (PageViewControlState)e.NewValue);
        }

        public PageViewControl()
        {
            this.InitializeComponent();

            observer.Start();
        }

        class PageLoadObserver
        {
            private Dictionary<PageViewControlState, Action<DjvuPage>> states = new Dictionary<PageViewControlState, Action<DjvuPage>>();

            readonly DispatcherTimer _timer = new DispatcherTimer();

            public PageLoadObserver()
            {
                _timer.Interval = TimeSpan.FromMilliseconds(30);
                _timer.Tick += Timer_Tick;
            }

            public void Subscribe(PageViewControlState state, Action<DjvuPage> callback)
            {
                states[state] = callback;
            }

            public void Unsubscribe(PageViewControlState state)
            {
                if (states.ContainsKey(state))
                {
                    states.Remove(state);
                }
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
                var state = pair.Key;
                var callback = pair.Value;

                Stop();
                var page = await state.Document.GetPageAsync(state.PageNumber);
                Start();

                if (states.ContainsKey(state))
                {
                    states.Remove(state);
                    callback(page);
                }
            }
        }

        private void PageDecodedHandler(DjvuPage page)
        {
            _page = page;

            _zoomFactorObserver = State.ZoomFactorObserver;
            _zoomFactorObserver.ZoomFactorChanging += HandleZoomFactorChanging;
            _zoomFactorObserver.ZoomFactorChanged += HandleZoomFactorChanged;

            Width = State.Width;
            Height = State.Height;

            CreateThumbnailSurface();

            if (!_zoomFactorObserver.IsZooming)
            {
                CreateContentSurface();
            }
        }

        private void OnStateChanged(PageViewControlState oldValue, PageViewControlState newValue)
        {
            CleanUp();

            if (oldValue != null)
            {
                observer.Unsubscribe(oldValue);
            }
            
            if (newValue != null)
            {
                observer.Subscribe(newValue, PageDecodedHandler);
            }
        }

        private void CleanUp()
        {
            if (_zoomFactorObserver != null)
            {
                _zoomFactorObserver.ZoomFactorChanging -= HandleZoomFactorChanging;
                _zoomFactorObserver.ZoomFactorChanged -= HandleZoomFactorChanged;
                _zoomFactorObserver = null;
            }
            
            if (_contentVsis != null)
            {
                _contentVsis.Dispose();
                _contentVsis = null;
            }

            if (_thumbnailSis != null)
            {
                _thumbnailSis.Dispose();
                _thumbnailSis = null;
            }

            thumbnailContentCanvas.Background = null;
            contentCanvas.Background = null;
            _page = null;
        }

        private void HandleZoomFactorChanging()
        {
            if (_contentVsis != null)
            {
                _contentVsis.Dispose();
                _contentVsis = null;
            }
        }

        private void HandleZoomFactorChanged()
        {
            CreateContentSurface();
        }

        private void CreateContentSurface()
        {
            var zoomFactor = _zoomFactorObserver.ZoomFactor;
            var pageViewSize = new Size(Width * zoomFactor, Height * zoomFactor);

            _contentVsis = new VsisWrapper(_page, _renderer.Value, pageViewSize);
            _contentVsis.CreateSurface();

            var contentBackgroundBrush = new ImageBrush
            {
                ImageSource = _contentVsis.Source
            };

            contentCanvas.Background = contentBackgroundBrush;
        }

        private void CreateThumbnailSurface()
        {
            const uint scaleFactor = 16;
            var pageViewSize = new Size(Width / scaleFactor, Height / scaleFactor);

            _thumbnailSis = new SisWrapper(_page, _renderer.Value, pageViewSize);
            _thumbnailSis.CreateSurface();

            var thumbnailBackgroundBrush = new ImageBrush
            {
                ImageSource = _thumbnailSis.Source
            };

            thumbnailContentCanvas.Background = thumbnailBackgroundBrush;
        }
    }
}
