using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
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

        private VsisPageRenderer _contentVsis;
        private SisPageRenderer _thumbnailSis;
        private DjvuPage _page;
        private PageViewObserver _pageViewObserver;
        private CancellationTokenSource _pageDecodingCts;
        public IReadOnlyCollection<TextLayerZone> TextLayer;

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

            _page = page;

            _pageViewObserver = State.ZoomFactorObserver;
            _pageViewObserver.ZoomFactorChanging += HandleZoomFactorChanging;
            _pageViewObserver.ZoomFactorChanged += HandleZoomFactorChanged;
            _pageViewObserver.SelectionChanging += HandleSelectionChanging;
            _pageViewObserver.SearchHighlightingRedrawingRequested += HandleSearchHighlightingRedrawingRequested;

            Width = State.Width;
            Height = State.Height;

            CreateThumbnailSurface();

            if (!_pageViewObserver.IsZooming)
            {
                CreateContentSurface();
            }

            TextLayer = textLayer != null ? new[] { textLayer } : Array.Empty<TextLayerZone>();

            RedrawSelection();
            RedrawSearchHighlighting();
        }

        private void HandleSearchHighlightingRedrawingRequested(object sender, EventArgs e)
        {
            RedrawSearchHighlighting();
        }

        private void HandleSelectionChanging()
        {
            RedrawSelection();
        }

        private static IEnumerable<TextLayerZone> GetSearchZones(IEnumerable<TextLayerZone> zones, string query)
        {
            return SearchHelper.Search(zones, query).SelectMany(zone => zone);
        }

        private void RedrawSearchHighlighting()
        {
            searchHighlightingShape.Data = null;

            if (_pageViewObserver.SearchText == null)
            {
                return;
            }

            var zones = GetSearchZones(TextLayer, _pageViewObserver.SearchText);
            searchHighlightingShape.Data = GetGeometryFromZones(zones);
        }

        Geometry GetGeometryFromZones(IEnumerable<TextLayerZone> zones)
        {
            var geometryGroup = new GeometryGroup();

            foreach (var zone in zones)
            {
                var rect = PageRectToDipRect(zone.Bounds);
                geometryGroup.Children.Add(new RectangleGeometry { Rect = rect });
            }

            return geometryGroup;
        }

        public static bool GetSelectionIndicesForPage(
            uint pageNumber,
            SelectionMarker selectionStart,
            SelectionMarker selectionEnd,
            out uint selectionStartIndex,
            out uint selectionEndIndex)
        {
            if (selectionStart > selectionEnd)
            {
                var tmp = selectionStart;
                selectionStart = selectionEnd;
                selectionEnd = tmp;
            }

            selectionStartIndex = 0;
            selectionEndIndex = 0;

            if (selectionStart.PageNumber < pageNumber)
            {
                selectionStartIndex = 0;
            }
            else if (selectionStart.PageNumber == pageNumber)
            {
                selectionStartIndex = selectionStart.Index;
            }
            else
            {
                return false;
            }

            if (selectionEnd.PageNumber > pageNumber)
            {
                selectionEndIndex = uint.MaxValue;
            }
            else if (selectionEnd.PageNumber == pageNumber)
            {
                selectionEndIndex = selectionEnd.Index;
            }
            else
            {
                return false;
            }

            return true;
        }

        private void RedrawSelection()
        {
            selectionShape.Data = null;

            if (!_pageViewObserver.IsSelected)
            {
                return;
            }

            uint selectionStartIndex, selectionEndIndex;
            if (!GetSelectionIndicesForPage(
                pageNumber: State.PageNumber,
                selectionStart: _pageViewObserver.SelectionStart,
                selectionEnd: _pageViewObserver.SelectionEnd,
                selectionStartIndex: out selectionStartIndex,
                selectionEndIndex: out selectionEndIndex))
            {
                return;
            }

            var selectionZones = GetSelectionZones(TextLayer, selectionStartIndex, selectionEndIndex);
            selectionShape.Data = GetGeometryFromZones(selectionZones);
        }

        Rect PageRectToDipRect(Rect pageRect)
        {
            var scaleFactor = Width / _page.Width;

            return new Rect(
                x: pageRect.X * scaleFactor,
                y: (_page.Height - pageRect.Bottom) * scaleFactor,
                width: pageRect.Width * scaleFactor,
                height: pageRect.Height * scaleFactor);
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
                _pageViewObserver.SelectionChanging -= HandleSelectionChanging;
                _pageViewObserver.SearchHighlightingRedrawingRequested -= HandleSearchHighlightingRedrawingRequested;
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
            selectionShape.Data = null;
            searchHighlightingShape.Data = null;
            _page = null;
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

            _contentVsis = new VsisPageRenderer(_page, pageViewSize);

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
            var pageWidth = _page.Width / rawPixelsPerViewPixel;
            var pageHeight = _page.Height / rawPixelsPerViewPixel;
            var pageViewSize = new Size(pageWidth / scaleFactor, pageHeight / scaleFactor);

            _thumbnailSis = new SisPageRenderer(_page, pageViewSize);

            var thumbnailBackgroundBrush = new ImageBrush
            {
                ImageSource = _thumbnailSis.Source
            };

            thumbnailContentCanvas.Background = thumbnailBackgroundBrush;
        }

        IEnumerable<TextLayerZone> GetSelectionZones(IEnumerable<TextLayerZone> zones, uint selectionStart, uint selectionEnd)
        {
            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Line || zone.Type == ZoneType.Word)
                {
                    if (selectionStart <= zone.StartIndex && zone.EndIndex <= selectionEnd)
                    {
                        yield return zone;
                    }
                    else
                    {
                        foreach (var childZone in GetSelectionZones(zone.Children, selectionStart, selectionEnd))
                        {
                            yield return childZone;
                        }
                    }
                }
                else
                {
                    foreach (var childZone in GetSelectionZones(zone.Children, selectionStart, selectionEnd))
                    {
                        yield return childZone;
                    }
                }

            }
        }

        public TextLayerZone FindWordAtPoint(IReadOnlyCollection<TextLayerZone> zones, Point point)
        {
            if (_page == null)
            {
                return null;
            }

            var scaleFactor = _page.Width / Width;
            var pagePoint = new Point(point.X * scaleFactor, _page.Height - point.Y * scaleFactor);

            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Word && zone.Bounds.Contains(pagePoint))
                {
                    return zone;
                }

                var result = FindWordAtPoint(zone.Children, point);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static readonly CoreCursor HoverCursor = new CoreCursor(CoreCursorType.IBeam, 0);
        private static readonly CoreCursor NormalCursor = new CoreCursor(CoreCursorType.Arrow, 0);

        private void PointerMovedHandler(object sender, PointerRoutedEventArgs e)
        {
            var zone = FindWordAtPoint(TextLayer, e.GetCurrentPoint(this).Position);
            CoreWindow.GetForCurrentThread().PointerCursor = zone != null ? HoverCursor : NormalCursor;
        }

        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            CoreWindow.GetForCurrentThread().PointerCursor = NormalCursor;

            _contentVsis?.Dispose();
            _contentVsis = null;
        }
    }
}
