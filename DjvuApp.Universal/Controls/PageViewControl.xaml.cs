using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
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
        private PageViewObserver _zoomFactorObserver;
        private int? _id;
        private IReadOnlyCollection<TextLayerZone> _textLayer;

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl)d;
            sender.OnStateChanged((PageViewControlState)e.OldValue, (PageViewControlState)e.NewValue);
        }

        public PageViewControl()
        {
            this.InitializeComponent();
        }
        
        public void PageDecodedHandler(DjvuPage page, TextLayerZone textLayer)
        {
            _page = page;

            _zoomFactorObserver = State.ZoomFactorObserver;
            _zoomFactorObserver.ZoomFactorChanging += HandleZoomFactorChanging;
            _zoomFactorObserver.ZoomFactorChanged += HandleZoomFactorChanged;
            _zoomFactorObserver.SelectionChanged += HandleSelectionChanged;

            Width = State.Width;
            Height = State.Height;

            CreateThumbnailSurface();

            if (!_zoomFactorObserver.IsZooming)
            {
                CreateContentSurface();
            }

            if (textLayer != null)
            {
                _textLayer = new[] { textLayer };
                _zonesOverlays = new Dictionary<TextLayerZone, UIElement>();
                CreateOverlays(new[] { textLayer });
            }
        }

        private void HandleSelectionChanged()
        {
            RedrawSelection();
        }

        private void RedrawSelection()
        {
            if (_zoomFactorObserver == null)
            {
                return;
            }

            foreach (var border in _zonesOverlays.Values)
            {
                border.Opacity = 0;
            }

            foreach (var zone in GetSelectionZones(_textLayer))
            {
                _zonesOverlays[zone].Opacity = 1;
            }
        }

        private void OnStateChanged(PageViewControlState oldValue, PageViewControlState newValue)
        {
            CleanUp();

            if (_id != null)
            {
                PageLoadScheduler.Instance.Unsubscribe(_id.Value);
                _id = null;
            }

            if (newValue != null)
            {
                _id = PageLoadScheduler.Instance.Subscribe(newValue, PageDecodedHandler);
            }
        }

        private void CleanUp()
        {
            if (_zoomFactorObserver != null)
            {
                _zoomFactorObserver.ZoomFactorChanging -= HandleZoomFactorChanging;
                _zoomFactorObserver.ZoomFactorChanged -= HandleZoomFactorChanged;
                _zoomFactorObserver.SelectionChanged -= HandleSelectionChanged;
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

        private Pointer _pointer;

        private void PageViewControl_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _zoomFactorObserver.IsSelected = false;

            var point = e.GetCurrentPoint(this).Position;
            var startingZone = FindWordAtPoint(_textLayer, point);
            if (startingZone == null)
            {
                return;
            }
            
            if (CapturePointer(e.Pointer))
            {
                _pointer = e.Pointer;
                var selectionStart = new SelectionMarker(State.PageNumber, startingZone.StartIndex);
                _zoomFactorObserver.SelectionStart = selectionStart;
                _zoomFactorObserver.SelectionEnd = selectionStart;
            }
        }

        private void PageViewControl_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_pointer != null)
            {
                ReleasePointerCapture(_pointer);
                _pointer = null;

                _zoomFactorObserver.IsSelected = true;
            }
        }

        private Dictionary<TextLayerZone, UIElement> _zonesOverlays;

        void CreateOverlays(IReadOnlyCollection<TextLayerZone> zones)
        {
            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Word || zone.Type == ZoneType.Line)
                {
                    var scaleFactor = Width / _page.Width;

                    var zoneOverlay = new Rectangle
                    {
                        Fill = (Brush) Resources["SelectionBackgroundBrush"],
                        Opacity = 0,
                        Width = zone.Bounds.Width * scaleFactor,
                        Height = zone.Bounds.Height * scaleFactor
                    };
                    zoneOverlay.PointerEntered += (sender, args) =>
                    {
                        CoreWindow.GetForCurrentThread().PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
                    };
                    zoneOverlay.PointerExited += (sender, args) =>
                    {
                        CoreWindow.GetForCurrentThread().PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                    };
                    Canvas.SetLeft(zoneOverlay, zone.Bounds.X * scaleFactor);
                    Canvas.SetTop(zoneOverlay, (_page.Height - zone.Bounds.Bottom) * scaleFactor);
                    contentCanvas.Children.Add(zoneOverlay);

                    _zonesOverlays[zone] = zoneOverlay;
                }

                CreateOverlays(zone.Children);
            }
        }
        
        IEnumerable<TextLayerZone> GetSelectionZones(IReadOnlyCollection<TextLayerZone> zones)
        {
            var startIndex = Math.Min(_zoomFactorObserver.SelectionStart.Index, _zoomFactorObserver.SelectionEnd.Index);
            var endIndex = Math.Max(_zoomFactorObserver.SelectionStart.Index, _zoomFactorObserver.SelectionEnd.Index);

            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Line || zone.Type == ZoneType.Word)
                {
                    if (startIndex <= zone.StartIndex && zone.EndIndex <= endIndex)
                    {
                        yield return zone;
                    }
                    else
                    {
                        foreach (var childZone in GetSelectionZones(zone.Children))
                        {
                            yield return childZone;
                        }
                    }
                }
                else
                {
                    foreach (var childZone in GetSelectionZones(zone.Children))
                    {
                        yield return childZone;
                    }
                }
                
            }
        }

        private void PageViewControl_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_pointer == null)
            {
                return;
            }

            var point = e.GetCurrentPoint(this).Position;
            var endingZone = FindWordAtPoint(_textLayer, point);
            if (endingZone == null)
            {
                return;
            }
            _zoomFactorObserver.SelectionEnd = new SelectionMarker(State.PageNumber, endingZone.EndIndex);

            _zoomFactorObserver.RaiseSelectionChanged();
        }
        
        TextLayerZone FindWordAtPoint(IReadOnlyCollection<TextLayerZone> zones, Point point)
        {
            var scaleFactor = _page.Width / Width;
            var pagePoint = new Point(point.X * scaleFactor, _page.Height - point.Y * scaleFactor);
            
            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Word && zone.Bounds.Contains(pagePoint))
                {
                    return zone;
                }
                else
                {
                    var result = FindWordAtPoint(zone.Children, point);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        void BuildSelectionText(IReadOnlyCollection<TextLayerZone> zones, StringBuilder stringBuilder)
        {
            var startIndex = Math.Min(_zoomFactorObserver.SelectionStart.Index, _zoomFactorObserver.SelectionEnd.Index);
            var endIndex = Math.Max(_zoomFactorObserver.SelectionStart.Index, _zoomFactorObserver.SelectionEnd.Index);

            foreach (var zone in zones)
            {
                if (startIndex <= zone.StartIndex && zone.EndIndex <= endIndex)
                {
                    if (zone.Type == ZoneType.Paragraph || zone.Type == ZoneType.Line)
                    {
                        BuildSelectionText(zone.Children, stringBuilder);
                        stringBuilder.AppendLine();
                    }
                    else if (zone.Type == ZoneType.Word)
                    {
                        stringBuilder.Append(zone.Text);
                        stringBuilder.Append(' ');
                    }
                }
                else
                {
                    BuildSelectionText(zone.Children, stringBuilder);
                }
            }
        }

        public string GetSelectionText()
        {
            var stringBuilder = new StringBuilder();

            BuildSelectionText(_textLayer, stringBuilder);

            return stringBuilder.ToString();
        }
    }
}
