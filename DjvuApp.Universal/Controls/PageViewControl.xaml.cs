using System;
using System.Diagnostics;
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

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl) d;
            sender.OnStateChanged();
        }

        public PageViewControl()
        {
            this.InitializeComponent();
        }

        private async void OnStateChanged()
        {
            CleanUp();

            if (State == null)
            {
                return;
            }

            var state = State;
            _page = State.Document.GetPage(State.PageNumber);

            if (state != State)
            {
                return;
            }

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
