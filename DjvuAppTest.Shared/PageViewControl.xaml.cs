using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Djvu;

namespace DjvuApp
{
    public class ZoomFactorObserver
    {
        private double _zoomFactor = 1;

        public double ZoomFactor
        {
            get
            {
                return _zoomFactor;
            }
            set
            {
                _zoomFactor = value;
                RaiseZoomFactorChanged();
            }
        }

        public event Action ZoomFactorChanged;

        private void RaiseZoomFactorChanged()
        {
            var handler = ZoomFactorChanged;
            if (handler != null) handler();
        }
    }

    public class PageViewControlState
    {
        public DjvuDocument Document { get; set; }
        public uint PageNumber { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public ZoomFactorObserver ZoomFactorObserver { get; set; }
    }

    public sealed partial class PageViewControl : UserControl
    {
        public static Renderer Renderer;

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
        private ZoomFactorObserver _zoomFactorObserver;

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl) d;
            sender.OnStateChanged();
        }

        private void OnStateChanged()
        {
            Cleanup();

            if (State == null)
                return;

            _zoomFactorObserver = State.ZoomFactorObserver;
            _zoomFactorObserver.ZoomFactorChanged += HandleZoomFactorChanged;

            Width = State.Width;
            Height = State.Height;
            
            blankContentCanvas.Opacity = 1;
            thumbnailContentCanvas.Opacity = 0;
            contentCanvas.Opacity = 0;

            if (_page != null)
            {
                throw new Exception();
            }

            _page = State.Document.GetPage(State.PageNumber);
            CreateThumbnailSurface();

            blankContentCanvas.Opacity = 0;
            thumbnailContentCanvas.Opacity = 1;

            CreateContentSurface();

            contentCanvas.Opacity = 1;
        }

        private void Cleanup()
        {
            if (_zoomFactorObserver != null)
            {
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

        private void HandleZoomFactorChanged()
        {
            _contentVsis = null;
            CreateContentSurface();
        }

        private void CreateContentSurface()
        {
            if (_contentVsis != null)
            {
                throw new Exception();
            }

            var zoomFactor = _zoomFactorObserver.ZoomFactor;
            var pageViewSize = new Size(Width * zoomFactor, Height * zoomFactor);
            _contentVsis = new VsisWrapper(_page, Renderer, pageViewSize);
            _contentVsis.CreateSurface();

            var contentBackgroundBrush = new ImageBrush
            {
                ImageSource = _contentVsis.Source
            };

            contentCanvas.Background = contentBackgroundBrush;
        }

        private void CreateThumbnailSurface()
        {
            if (_thumbnailSis != null)
            {
                throw new Exception();
            }

            var pageViewSize = new Size(Width / 16, Height / 16);
            _thumbnailSis = new SisWrapper(_page, Renderer, pageViewSize);
            _thumbnailSis.CreateSurface();

            var thumbnailBackgroundBrush = new ImageBrush
            {
                ImageSource = _thumbnailSis.Source
            };

            thumbnailContentCanvas.Background = thumbnailBackgroundBrush;
        }

        public PageViewControl()
        {
            this.InitializeComponent();
        }
    }
}
