using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed partial class ReaderControl : UserControl
    {
        private class ZoomFactorObserver : IZoomFactorObserver
        {
            public bool IsZooming { get; private set; }

            public float ZoomFactor { get; private set; }

            public event Action ZoomFactorChanging;

            public event Action ZoomFactorChanged;

            public ZoomFactorObserver()
            {
                ZoomFactor = 1;
            }

            public void OnZoomFactorChanged(float zoomFactor, bool isIntermediate)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (zoomFactor != ZoomFactor && !IsZooming)
                {
                    RaiseZoomFactorChanging();
                    IsZooming = true;
                }

                if (!isIntermediate && IsZooming)
                {
                    ZoomFactor = zoomFactor;
                    RaiseZoomFactorChanged();
                    IsZooming = false;
                }
            }

            private void RaiseZoomFactorChanged()
            {
                var handler = ZoomFactorChanged;
                if (handler != null) handler();
            }

            private void RaiseZoomFactorChanging()
            {
                var handler = ZoomFactorChanging;
                if (handler != null) handler();
            }
        }

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

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(DjvuDocument), typeof(ReaderControl), new PropertyMetadata(null, SourceChangedCallback));

        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register("PageNumber", typeof(uint), typeof(ReaderControl), new PropertyMetadata(0U, PageNumberChangedCallback));

        private bool _supressPageNumberChangedCallback;
        private ZoomFactorObserver _zoomFactorObserver;
        private ScrollViewer _scrollViewer;
        private Size? _containerSize;
        private PageViewControlState[] _pageStates;

        public ReaderControl()
        {
            this.InitializeComponent();
        }

        private void OnPageNumberChanged()
        {
            if (Source == null)
                throw new InvalidOperationException("Source is null.");
            if (PageNumber == 0 || PageNumber > Source.PageCount)
                throw new InvalidOperationException("PageNumber is out of range.");

            if (_supressPageNumberChangedCallback)
                return;

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

            _scrollViewer.ChangeView(horizontalOffset, verticalOffset, (float) zoomFactor, true);
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

        private void Load()
        {
            if (Source == null || _containerSize == null)
            {
                return;
            }

            _zoomFactorObserver = new ZoomFactorObserver();

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
                    zoomFactorObserver: _zoomFactorObserver);
            }
            
            SetPageNumberWithoutNotification(1);
            listView.ItemsSource = _pageStates;
        }

        private void Unload()
        {
            _zoomFactorObserver = null;
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
            var sender = (ReaderControl) d;
            sender.OnSourceChanged();
        }

        private static void PageNumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (ReaderControl) d;
            sender.OnPageNumberChanged();
        }

        private void ScrollViewerLoadedHandler(object sender, RoutedEventArgs e)
        {
            _scrollViewer = (ScrollViewer) sender;
            _scrollViewer.ViewChanged += ViewChangedHandler;
        }

        private void ViewChangedHandler(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // For some reason, VerticalOffset == top_item_index + 2.
            var topPageNumber = _scrollViewer.VerticalOffset - 1;
            var visiblePagesCount = _scrollViewer.ViewportHeight;
            var middlePageNumber = topPageNumber + visiblePagesCount / 2;

            SetPageNumberWithoutNotification((uint) middlePageNumber);

            _zoomFactorObserver.OnZoomFactorChanged(_scrollViewer.ZoomFactor, e.IsIntermediate);
        }

        private void SetPageNumberWithoutNotification(uint value)
        {
            _supressPageNumberChangedCallback = true;
            PageNumber = value;
            _supressPageNumberChangedCallback = false;
        }
    }
}
