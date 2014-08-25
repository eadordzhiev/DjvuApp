using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Misc;
using DjvuLibRT;

namespace DjvuApp.Controls
{
    public sealed partial class DocumentViewer : UserControl
    {
        public DocumentViewer()
        {
            InitializeComponent();
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
            DependencyProperty.Register("Source", typeof(DjvuDocument), typeof(DocumentViewer), new PropertyMetadata(null, SourceChangedCallback));

        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register("PageNumber", typeof(uint), typeof(DocumentViewer), new PropertyMetadata(0U, PageNumberChangedCallback));

        private DjvuDocumentSource _viewModel;
        private ScrollViewer _scrollViewer;
        private VirtualizingStackPanel _virtualizingStackPanel;
        private bool _isPageNumberChangedCallbackSuppressed;

        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            if (Source == null)
                return;

            UpdateZoomConstraints(false);
        }

        private DjvuPageSource GetPage(uint pageNumber)
        {
            var index = (int)(pageNumber - 1);
            return _viewModel[index];
        }

        private float GetNormalZoomFactor(float width)
        {
            var viewportWidth = (float)_scrollViewer.ViewportWidth;
            var zoomFactor = viewportWidth / width;

            if (zoomFactor < 0.1f)
                zoomFactor = 0.1f;

            return zoomFactor;
        }

        private void UpdateZoomConstraints(bool afterSourceChanged)
        {
            var normalZoomFactor = GetNormalZoomFactor(_viewModel.MaxWidth);
            var minZoomFactor = normalZoomFactor / 2;
            var maxZoomFactor = 1;

            // ScrollViewer throws an exception
            // if MinZoomFactor is less than 0.1
            if (minZoomFactor < 0.1f)
                minZoomFactor = 0.1f;
            
            _scrollViewer.MinZoomFactor = minZoomFactor;
            _scrollViewer.MaxZoomFactor = maxZoomFactor;

            // Zooming bug workaround
            // Any offset greater than 2
            // fixes that issue
            _scrollViewer.ChangeView(
                horizontalOffset: null, 
                verticalOffset: afterSourceChanged 
                    ? 2.000001D 
                    : _scrollViewer.VerticalOffset, 
                zoomFactor: normalZoomFactor, 
                disableAnimation: true);
        }

        private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (Source == null)
            {
                listView.ItemsSource = _viewModel = null;
                return;
            }

            listView.ItemsSource = _viewModel = new DjvuDocumentSource(Source);

            UpdateZoomConstraints(true);
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
            // Due to unknown behavior of ListView,
            // more at ViewChangedHandler
            double offset = pageNumber + 1;
            
            // We need to switch page first
            // in order to get right ViewportHeight
            // if it differs from the current
            _scrollViewer.ChangeView(0, offset, null, true);

            var pageOffset = (_scrollViewer.ViewportHeight - 1) / 2;
            if (pageOffset > 0)
                offset -= pageOffset;

            var page = GetPage(pageNumber);
            var zoomFactor = GetNormalZoomFactor(page.Width);

            // Now we have centered offset and right zoomFactor
            // so we can finally change view
            _scrollViewer.ChangeView(0, offset, zoomFactor, true);
        }

        private static void SourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (DocumentViewer)d;
            sender.OnSourceChanged(e);
        }

        private static void PageNumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (DocumentViewer)d;
            sender.OnPageNumberChanged(e);
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            SizeChanged += SizeChangedHandler;
        }

        private void LayoutUpdatedHandler(object sender, object e)
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = listView.GetVisualTreeChildren<ScrollViewer>().FirstOrDefault(control => control.Name == "ScrollViewer");
                if (_scrollViewer != null)
                {
                    _scrollViewer.ViewChanged += ViewChangedHandler;
                }
            }

            if (_virtualizingStackPanel == null)
            {
                _virtualizingStackPanel = (VirtualizingStackPanel)listView.ItemsPanelRoot;
                if (_virtualizingStackPanel != null)
                {
                    _virtualizingStackPanel.CleanUpVirtualizedItemEvent += CleanUpVirtualizedItemEventHandler;
                }
            }
        }

        private void ViewChangedHandler(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // For some reason, VerticalOffset == top item index + 2.
            var topPageNumber = _scrollViewer.VerticalOffset - 1;
            var visiblePagesCount = _scrollViewer.ViewportHeight;
            var middlePageNumber = topPageNumber + visiblePagesCount / 2;

            // Suppress PageNumberChangedCallback
            // to prevet accidental changing of the page
            _isPageNumberChangedCallbackSuppressed = true;
            PageNumber = (uint)Math.Floor(middlePageNumber);
            _isPageNumberChangedCallbackSuppressed = false;
        }

        private void CleanUpVirtualizedItemEventHandler(object sender, CleanUpVirtualizedItemEventArgs e)
        {
            var item = e.Value as IDisposable;
            if (item != null)
            {
                item.Dispose();
            }
        }
    }
}
