using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
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
        private int? _id;

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl) d;
            sender.OnStateChanged((PageViewControlState) e.OldValue, (PageViewControlState)e.NewValue);
        }

        public PageViewControl()
        {
            this.InitializeComponent();
        }

        void ProcessZones(IReadOnlyCollection<TextLayerZone> zones)
        {
            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Word)
                {
                    var scaleFactor = Width / _page.Width;

                    var textBlock = new TextBlock();
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.Text = zone.Text;
                    textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                    textBlock.VerticalAlignment = VerticalAlignment.Center;
                    var border = new Border();
                    border.Background = new SolidColorBrush(Color.FromArgb(0xAF, 0, 0, 0));
                    border.Child = textBlock;
                    border.Width = zone.Bounds.Width * scaleFactor;
                    border.Height = zone.Bounds.Height * scaleFactor;
                    Canvas.SetLeft(border, zone.Bounds.X * scaleFactor);
                    Canvas.SetTop(border, (_page.Height - zone.Bounds.Bottom) * scaleFactor);
                    contentCanvas.Children.Add(border);
                }
                else
                {
                    ProcessZones(zone.Children);
                }
            }
        }

        public void PageDecodedHandler(DjvuPage page, TextLayerZone textLayer)
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

            if (textLayer != null)
            {
                ProcessZones(new[] { textLayer });
            }
        }

        private void OnStateChanged(PageViewControlState oldValue, PageViewControlState newValue)
        {
            CleanUp();

            if (_id != null)
            {
                PageLoadObserver.Instance.Unsubscribe(_id.Value);
                _id = null;
            }

            if (newValue != null)
            {
                _id = PageLoadObserver.Instance.Subscribe(newValue, PageDecodedHandler);
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
            contentCanvas.Children.Clear();
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
