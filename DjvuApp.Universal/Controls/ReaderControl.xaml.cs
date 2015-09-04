using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed partial class ReaderControl : UserControl
    {
        public DjvuDocument Source
        {
            get { return (DjvuDocument)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public uint PageNumber
        {
            get { return (uint)GetValue(PageNumberProperty); }
            set { SetValue(PageNumberProperty, value); }
        }

        public SelectionMarker SelectionStart { get; private set; }

        public SelectionMarker SelectionEnd { get; private set; }

        public bool IsSelected { get; private set; }

        public event EventHandler SelectionChanged;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(DjvuDocument), typeof(ReaderControl), new PropertyMetadata(null, SourceChangedCallback));

        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register("PageNumber", typeof(uint), typeof(ReaderControl), new PropertyMetadata(0U, PageNumberChangedCallback));

        private bool _supressPageNumberChangedCallback;
        private PageViewObserver _pageViewObserver;
        private ScrollViewer _scrollViewer;
        private Size? _containerSize;
        private PageViewControlState[] _pageStates;

        public ReaderControl()
        {
            this.InitializeComponent();

            listView.AddHandler(RightTappedEvent, new RightTappedEventHandler(RightTappedHandler), true);
        }

        private void RightTappedHandler(object sender, RightTappedRoutedEventArgs e)
        {
            if (!IsSelected)
            {
                return;
            }

            var element = (UIElement) sender;
            selectionContextMenu.ShowAt(element, e.GetPosition(element));
        }

        private void OnPageNumberChanged()
        {
            if (Source == null)
            {
                throw new InvalidOperationException("Source is null.");
            }
            if (PageNumber == 0 || PageNumber > Source.PageCount)
            {
                throw new InvalidOperationException("PageNumber is out of range.");
            }

            if (_supressPageNumberChangedCallback)
            {
                return;
            }
            
            GoToPage(PageNumber);
        }

        private void GoToPage(uint pageNumber)
        {
            if (Source == null || _containerSize == null)
            {
                return;
            }

            var pageState = _pageStates[pageNumber - 1];

            var zoomFactor = ActualWidth / pageState.Width;
            var verticalOffset = pageNumber + 1;
            var horizontalOffset = (ActualWidth - pageState.Width) / 2 * zoomFactor;

            _scrollViewer.ChangeView(horizontalOffset, verticalOffset, (float)zoomFactor, true);
        }

        private void OnSourceChanged()
        {
            if (Source != null)
            {
                Load();
            }
            else
            {
                Unload();
            }
        }

        private void RaiseSelectionChanged()
        {
            IsSelected = _pageViewObserver.IsSelected;
            SelectionStart = _pageViewObserver.SelectionStart;
            SelectionEnd = _pageViewObserver.SelectionEnd;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Load()
        {
            if (Source == null || _containerSize == null)
            {
                return;
            }

            _pageViewObserver = new PageViewObserver();

            var pageInfos = Source.GetPageInfos();
            var maxPageWidth = pageInfos.Max(pageInfo => pageInfo.Width);
            _pageStates = new PageViewControlState[Source.PageCount];

            for (uint i = 0; i < _pageStates.Length; i++)
            {
                var pageInfo = pageInfos[i];
                double pageWidth = pageInfo.Width;
                double pageHeight = pageInfo.Height;

                var scaleFactor = pageWidth / maxPageWidth;
                var aspectRatio = pageWidth / pageHeight;
                var width = scaleFactor * _containerSize.Value.Width;
                var height = width / aspectRatio;

                _pageStates[i] = new PageViewControlState(
                    document: Source,
                    pageNumber: i + 1,
                    width: width,
                    height: height,
                    zoomFactorObserver: _pageViewObserver);
            }

            SetPageNumberWithoutNotification(1);
            listView.ItemsSource = _pageStates;
        }

        private void Unload()
        {
            _pageViewObserver = null;
            _pageStates = null;
            listView.ItemsSource = null;
        }

        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            var oldContainerSize = _containerSize;
            _containerSize = new Size(ActualWidth, ActualHeight);

            if (ActualWidth != oldContainerSize?.Width)
            {
                Load();
            }
        }

        private static void SourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (ReaderControl)d;
            sender.OnSourceChanged();
        }

        private static void PageNumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (ReaderControl)d;
            sender.OnPageNumberChanged();
        }

        private void ScrollViewerLoadedHandler(object sender, RoutedEventArgs e)
        {
            _scrollViewer = (ScrollViewer)sender;
            _scrollViewer.ViewChanged += ViewChangedHandler;
            _scrollViewer.AddHandler(PointerPressedEvent, new PointerEventHandler(PageViewControl_OnPointerPressed), true);
        }

        private void ViewChangedHandler(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // For some reason, VerticalOffset == top_item_index + 2.
            var topPageNumber = _scrollViewer.VerticalOffset - 1;
            var visiblePagesCount = _scrollViewer.ViewportHeight;
            var middlePageNumber = topPageNumber + visiblePagesCount / 2;

            SetPageNumberWithoutNotification((uint)middlePageNumber);

            _pageViewObserver.OnZoomFactorChanged(_scrollViewer.ZoomFactor, e.IsIntermediate);
        }

        private void SetPageNumberWithoutNotification(uint value)
        {
            _supressPageNumberChangedCallback = true;
            PageNumber = value;
            _supressPageNumberChangedCallback = false;
        }

        private void PageViewControl_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            _pageViewObserver.IsSelected = false;
            _pageViewObserver.RaiseSelectionChanging();
            RaiseSelectionChanged();

            var pageViewControl = VisualTreeHelper
                .FindElementsInHostCoordinates(e.GetCurrentPoint(null).Position, this, true)
                .OfType<PageViewControl>()
                .FirstOrDefault();

            if (pageViewControl?.TextLayer == null)
            {
                return;
            }

            var startingZone = pageViewControl.FindWordAtPoint(pageViewControl.TextLayer, e.GetCurrentPoint(pageViewControl).Position);
            if (startingZone == null)
            {
                return;
            }

            if (_scrollViewer.CapturePointer(e.Pointer))
            {
                var selectionStart = new SelectionMarker(pageViewControl.State.PageNumber, startingZone.StartIndex);
                _pageViewObserver.SelectionStart = selectionStart;
                _pageViewObserver.SelectionEnd = selectionStart;

                _scrollViewer.PointerMoved += PageViewControl_OnPointerMoved;
                _scrollViewer.PointerReleased += PageViewControl_OnPointerReleased;
            }
        }

        private void PageViewControl_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var pageViewControl = VisualTreeHelper
                .FindElementsInHostCoordinates(e.GetCurrentPoint(null).Position, this, true)
                .OfType<PageViewControl>()
                .FirstOrDefault();

            if (pageViewControl?.TextLayer == null)
            {
                return;
            }

            var endingZone = pageViewControl.FindWordAtPoint(pageViewControl.TextLayer, e.GetCurrentPoint(pageViewControl).Position);
            if (endingZone == null)
            {
                return;
            }

            _pageViewObserver.IsSelected = true;
            _pageViewObserver.SelectionEnd = new SelectionMarker(pageViewControl.State.PageNumber, endingZone.EndIndex);
            _pageViewObserver.RaiseSelectionChanging();
        }

        private void PageViewControl_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _scrollViewer.PointerMoved -= PageViewControl_OnPointerMoved;
            _scrollViewer.PointerReleased -= PageViewControl_OnPointerReleased;
            
            RaiseSelectionChanged();
        }

        private static void BuildSelectionText(IReadOnlyCollection<TextLayerZone> zones, uint selectionStartIndex, uint selectionEndIndex, StringBuilder stringBuilder)
        {
            foreach (var zone in zones)
            {
                if (selectionStartIndex <= zone.StartIndex && zone.EndIndex <= selectionEndIndex)
                {
                    switch (zone.Type)
                    {
                        case ZoneType.Paragraph:
                        case ZoneType.Line:
                            BuildSelectionText(zone.Children, selectionStartIndex, selectionEndIndex, stringBuilder);
                            stringBuilder.AppendLine();
                            break;
                        case ZoneType.Word:
                            stringBuilder.Append(zone.Text);
                            stringBuilder.Append(' ');
                            break;
                        default:
                            BuildSelectionText(zone.Children, selectionStartIndex, selectionEndIndex, stringBuilder);
                            break;
                    }
                }
                else
                {
                    BuildSelectionText(zone.Children, selectionStartIndex, selectionEndIndex, stringBuilder);
                }
            }
        }
        
        private async void CopyButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var startingPage = Math.Min(SelectionStart.PageNumber, SelectionEnd.PageNumber);
            var endingPage = Math.Max(SelectionStart.PageNumber, SelectionEnd.PageNumber);
            var stringBuilder = new StringBuilder();

            for (var pageNumber = startingPage; pageNumber <= endingPage; pageNumber++)
            {
                uint selectionStartIndex, selectionEndIndex;

                if (!PageViewControl.GetSelectionIndicesForPage(pageNumber, SelectionStart, SelectionEnd, out selectionStartIndex, out selectionEndIndex))
                {
                    continue;
                }
                
                var textLayer = await Source.GetTextLayerAsync(pageNumber);
                if (textLayer == null)
                {
                    continue;
                }

                BuildSelectionText(new[] { textLayer }, selectionStartIndex, selectionEndIndex, stringBuilder);
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(stringBuilder.ToString());

            Clipboard.SetContent(dataPackage);
        }
    }
}
