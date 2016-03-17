using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed partial class PageViewControl : UserControl
    {
        public PageViewControlState State
        {
            get { return (PageViewControlState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(PageViewControlState), typeof(PageViewControl), new PropertyMetadata(null, StateChangedCallback));
        
        public IReadOnlyCollection<TextLayerZone> TextLayer
        {
            get { return (IReadOnlyCollection<TextLayerZone>)GetValue(TextLayerProperty); }
            set { SetValue(TextLayerProperty, value); }
        }
        
        public static readonly DependencyProperty TextLayerProperty =
            DependencyProperty.Register("TextLayer", typeof(IReadOnlyCollection<TextLayerZone>), typeof(PageViewControl), new PropertyMetadata(null));
        
        public DjvuPage Page
        {
            get { return (DjvuPage)GetValue(PageProperty); }
            set { SetValue(PageProperty, value); }
        }
        
        public static readonly DependencyProperty PageProperty =
            DependencyProperty.Register("Page", typeof(DjvuPage), typeof(PageViewControl), new PropertyMetadata(null));
        
        private VsisPageRenderer _contentVsis;
        private SisPageRenderer _thumbnailSis;
        private PageViewObserver _pageViewObserver;
        private CancellationTokenSource _pageDecodingCts;

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl)d;
            sender.OnStateChanged((PageViewControlState)e.OldValue, (PageViewControlState)e.NewValue);
        }

        public PageViewControl()
        {
            this.InitializeComponent();
        }

        private void PageDecodedHandler(DjvuPage page, TextLayerZone textLayer, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            Page = page;

            _pageViewObserver = State.ZoomFactorObserver;
            _pageViewObserver.ZoomFactorChanging += HandleZoomFactorChanging;
            _pageViewObserver.ZoomFactorChanged += HandleZoomFactorChanged;

            Width = State.Width;
            Height = State.Height;

            CreateThumbnailSurface();

            if (!_pageViewObserver.IsZooming)
            {
                CreateContentSurface();
            }

            TextLayer = textLayer != null ? new[] { textLayer } : Array.Empty<TextLayerZone>();
        }
        
        private void OnStateChanged(PageViewControlState oldValue, PageViewControlState newValue)
        {
            CleanUp();
            
            _pageDecodingCts?.Cancel();
            
            if (newValue != null)
            {
                _pageDecodingCts = new CancellationTokenSource();
                PageLoadScheduler.Instance.Subscribe(newValue, PageDecodedHandler, _pageDecodingCts.Token);
            }
        }

        private void CleanUp()
        {
            if (_pageViewObserver != null)
            {
                _pageViewObserver.ZoomFactorChanging -= HandleZoomFactorChanging;
                _pageViewObserver.ZoomFactorChanged -= HandleZoomFactorChanged;
                _pageViewObserver = null;
            }

            if (_contentVsis != null)
            {
                _contentVsis.Dispose();
                _contentVsis = null;
            }

            _thumbnailSis = null;
            thumbnailContentCanvas.Background = null;
            contentCanvas.Background = null;
            contentCanvas.Children.Clear();
            Page = null;
            TextLayer = null;
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
            var zoomFactor = _pageViewObserver.ZoomFactor;
            var pageViewSize = new Size(Width * zoomFactor, Height * zoomFactor);

            var thumbnailSize = _thumbnailSis.Source.Size;
            if (pageViewSize.Width < thumbnailSize.Width && pageViewSize.Height < thumbnailSize.Height)
            {
                return;
            }

            _contentVsis = new VsisPageRenderer(Page, pageViewSize);

            var contentBackgroundBrush = new ImageBrush
            {
                ImageSource = _contentVsis.Source
            };

            contentCanvas.Background = contentBackgroundBrush;
        }

        private void CreateThumbnailSurface()
        {
            const uint scaleFactor = 8;
            var rawPixelsPerViewPixel = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var pageWidth = Page.Width / rawPixelsPerViewPixel;
            var pageHeight = Page.Height / rawPixelsPerViewPixel;
            var pageViewSize = new Size(pageWidth / scaleFactor, pageHeight / scaleFactor);

            _thumbnailSis = new SisPageRenderer(Page, pageViewSize);

            var thumbnailBackgroundBrush = new ImageBrush
            {
                ImageSource = _thumbnailSis.Source
            };

            thumbnailContentCanvas.Background = thumbnailBackgroundBrush;
        }
        
        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            _contentVsis?.Dispose();
            _contentVsis = null;
        }
    }
}
