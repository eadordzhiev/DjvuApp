using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed partial class TextLayerControl : UserControl
    {
        public PageViewControlState State
        {
            get { return (PageViewControlState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public IReadOnlyCollection<TextLayerZone> TextLayer
        {
            get { return (IReadOnlyCollection<TextLayerZone>)GetValue(TextLayerProperty); }
            set { SetValue(TextLayerProperty, value); }
        }

        public DjvuPage Page
        {
            get { return (DjvuPage)GetValue(PageProperty); }
            set { SetValue(PageProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(PageViewControlState), typeof(TextLayerControl), new PropertyMetadata(null));

        public static readonly DependencyProperty TextLayerProperty =
            DependencyProperty.Register("TextLayer", typeof(IReadOnlyCollection<TextLayerZone>), typeof(TextLayerControl), new PropertyMetadata(null, TextLayerChangedCallback));

        public static readonly DependencyProperty PageProperty =
            DependencyProperty.Register("Page", typeof(DjvuPage), typeof(TextLayerControl), new PropertyMetadata(null));

        private static readonly CoreCursor HoverCursor = new CoreCursor(CoreCursorType.IBeam, 0);
        private static readonly CoreCursor NormalCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        private PageViewObserver _pageViewObserver;

        public TextLayerControl()
        {
            InitializeComponent();
        }

        public TextLayerZone FindWordAtPoint(IEnumerable<TextLayerZone> zones, Point point)
        {
            if (Page == null)
            {
                return null;
            }

            var scaleFactor = Page.Width / Width;
            var pagePoint = new Point(point.X * scaleFactor, Page.Height - point.Y * scaleFactor);

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

        private static void TextLayerChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (TextLayerControl)d;
            sender.OnTextLayerChanged();
        }

        private void OnTextLayerChanged()
        {
            if (TextLayer != null)
            {
                _pageViewObserver = State.ZoomFactorObserver;
                _pageViewObserver.SelectionChanging += HandleSelectionChanging;
                _pageViewObserver.SearchHighlightingRedrawingRequested += HandleSearchHighlightingRedrawingRequested;

                RedrawSelection();
                RedrawSearchHighlighting();
            }
            else
            {
                _pageViewObserver.SelectionChanging -= HandleSelectionChanging;
                _pageViewObserver.SearchHighlightingRedrawingRequested -= HandleSearchHighlightingRedrawingRequested;
                _pageViewObserver = null;

                selectionShape.Data = null;
                searchHighlightingShape.Data = null;
            }
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

        private Geometry GetGeometryFromZones(IEnumerable<TextLayerZone> zones)
        {
            var geometryGroup = new GeometryGroup();

            foreach (var zone in zones)
            {
                var rect = PageRectToDipRect(zone.Bounds);
                geometryGroup.Children.Add(new RectangleGeometry { Rect = rect });
            }

            return geometryGroup;
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

        private Rect PageRectToDipRect(Rect pageRect)
        {
            var scaleFactor = Width / Page.Width;

            return new Rect(
                x: pageRect.X * scaleFactor,
                y: (Page.Height - pageRect.Bottom) * scaleFactor,
                width: pageRect.Width * scaleFactor,
                height: pageRect.Height * scaleFactor);
        }

        private IEnumerable<TextLayerZone> GetSelectionZones(IEnumerable<TextLayerZone> zones, uint selectionStart, uint selectionEnd)
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

        private void PointerMovedHandler(object sender, PointerRoutedEventArgs e)
        {
            var zone = FindWordAtPoint(TextLayer, e.GetCurrentPoint(this).Position);
            CoreWindow.GetForCurrentThread().PointerCursor = zone != null ? HoverCursor : NormalCursor;
        }

        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            CoreWindow.GetForCurrentThread().PointerCursor = NormalCursor;
        }
    }
}
