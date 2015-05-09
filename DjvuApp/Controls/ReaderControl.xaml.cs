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

        private bool _isLoaded;
        private bool _isPageNumberChangedCallbackSuppressed;
        private ZoomFactorObserver _zoomFactorObserver;
        private ScrollViewer _scrollViewer;
        private double _maxPageWidth;
        private PageViewControlState[] _pageStates;

        public ReaderControl()
        {
            this.InitializeComponent();

            Loaded += LoadedHandler;
            Unloaded += UnloadedHandler;
            SizeChanged += SizeChangedHandler;
        }

        private void OnPageNumberChanged(DependencyPropertyChangedEventArgs e)
        {
            if (Source == null)
                throw new InvalidOperationException("Source is null.");
            if (PageNumber == 0 || PageNumber > Source.PageCount)
                throw new InvalidOperationException("PageNumber is out of range.");

            if (_isPageNumberChangedCallbackSuppressed)
                return;

            GoToPage(PageNumber);
        }

        private void GoToPage(uint pageNumber)
        {
            var pageState = _pageStates[pageNumber - 1];

            // I have no idea why, but it works.
            const double additionalHorizontalOffset = 18;

            var verticalOffset = pageNumber + 1;
            var horizontalOffset = (ActualWidth - pageState.Width) / 2 + additionalHorizontalOffset;
            var zoomFactor = ActualWidth / pageState.Width;

            _scrollViewer.ChangeView(horizontalOffset, verticalOffset, (float) zoomFactor, true);
        }

        private void OnSourceChanged()
        {
            if (!_isLoaded)
                return;

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
            if (Source == null)
                return;

            _zoomFactorObserver = new ZoomFactorObserver();

            var pageInfos = Source.GetPageInfos();
            _maxPageWidth = pageInfos.Max(pageInfo => pageInfo.Width);

            var containerSize = new Size(ActualWidth, ActualHeight);
            _pageStates = new PageViewControlState[Source.PageCount];

            for (uint i = 0; i < _pageStates.Length; i++)
            {
                var pageInfo = pageInfos[i];
                double pageWidth = pageInfo.Width;
                double pageHeight = pageInfo.Height;

                var scaleFactor = pageWidth / _maxPageWidth;
                var aspectRatio = pageWidth / pageHeight;
                var width = scaleFactor * containerSize.Width;
                var height = width / aspectRatio;

                _pageStates[i] = new PageViewControlState(
                    document: Source,
                    pageNumber: i + 1,
                    width: width,
                    height: height,
                    zoomFactorObserver: _zoomFactorObserver); ;
            }

            listView.ItemsSource = _pageStates;
        }

        private void Unload()
        {
            _zoomFactorObserver = null;
            listView.ItemsSource = null;
        }

        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            Load();
        }

        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
        }

        private static void SourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (ReaderControl) d;
            sender.OnSourceChanged();
        }

        private static void PageNumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (ReaderControl) d;
            sender.OnPageNumberChanged(e);
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

            if (!e.IsIntermediate)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_scrollViewer.ZoomFactor != _zoomFactorObserver.ZoomFactor)
                {
                    _zoomFactorObserver.ZoomFactor = _scrollViewer.ZoomFactor;
                }
            }
        }

        private void SetPageNumberWithoutNotification(uint value)
        {
            _isPageNumberChangedCallbackSuppressed = true;
            PageNumber = value;
            _isPageNumberChangedCallbackSuppressed = false;
        }
    }
}
