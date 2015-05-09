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
using DjvuApp.Djvu;

namespace DjvuApp
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
            
            // Now we have centered offset and right zoomFactor
            // so we can finally change view
            _scrollViewer.ChangeView(0, offset, null, true);
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
            double maxWidth = pageInfos.Max(pageInfo => pageInfo.Width);
            var containerSize = new Size(ActualWidth, ActualHeight);

            var states = new PageViewControlState[Source.PageCount];

            for (uint i = 0; i < states.Length; i++)
            {
                var pageInfo = pageInfos[i];
                var scaleFactor = pageInfo.Width / maxWidth;
                var aspectRatio = ((double) pageInfo.Width) / pageInfo.Height;
                var width = scaleFactor * containerSize.Width;
                var height = width / aspectRatio;
                var state = new PageViewControlState
                {
                    Document = Source,
                    PageNumber = i + 1,
                    Width = width,
                    Height = height,
                    ZoomFactorObserver = _zoomFactorObserver
                };
                states[i] = state;
            }

            listView.ItemsSource = states;
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
            // For some reason, VerticalOffset == top item index + 2.
            var topPageNumber = _scrollViewer.VerticalOffset - 1;
            var visiblePagesCount = _scrollViewer.ViewportHeight;
            var middlePageNumber = topPageNumber + visiblePagesCount / 2;

            // Suppress PageNumberChangedCallback
            // to prevet accidental changing of the page
            _isPageNumberChangedCallbackSuppressed = true;
            PageNumber = (uint) middlePageNumber;
            _isPageNumberChangedCallbackSuppressed = false;

            if (!e.IsIntermediate)
            {
                if (_scrollViewer.ZoomFactor != _zoomFactorObserver.ZoomFactor)
                {
                    _zoomFactorObserver.ZoomFactor = _scrollViewer.ZoomFactor;
                }
            }
        }
    }
}
