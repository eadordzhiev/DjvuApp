using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DjvuApp.ViewModel;
using DjvuLibRT;

namespace DjvuApp.Controls
{
    public sealed class DocumentViewer : Control
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
            DependencyProperty.Register("Source", typeof(DjvuDocument), typeof(DocumentViewer), new PropertyMetadata(null, SourceChangedCallback));

        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register("PageNumber", typeof(uint), typeof(DocumentViewer), new PropertyMetadata(0U, PageNumberChangedCallback));

        private ListView _listView;
        private DjvuDocumentViewModel _viewModel;
        private ScrollViewer _scrollViewer;

        public DocumentViewer()
        {
            this.DefaultStyleKey = typeof(DocumentViewer);
        }
        
        protected override void OnApplyTemplate()
        {
            _listView = (ListView) GetTemplateChild("listView");
            _listView.Loaded += ListViewLoadedHandler;
        }

        private void ListViewLoadedHandler(object sender, RoutedEventArgs e)
        {
            _scrollViewer = (ScrollViewer) VisualTreeHelper.GetChild(_listView, 0);
            SizeChanged += SizeChangedHandler;
        }

        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            if (Source == null)
                return;

            UpdateZoomConstraints();
        }

        private async void UpdateZoomConstraints()
        {
            var maxWidth = _viewModel.MaxWidth;
            var viewportWidth = _scrollViewer.ViewportWidth;

            var normalZoomFactor = (float) (viewportWidth / maxWidth);
            if (normalZoomFactor < 0.1f)
                normalZoomFactor = 0.1f;

            var minZoomFactor = normalZoomFactor / 2;
            if (minZoomFactor < 0.1f)
                minZoomFactor = 0.1f;

            const int maxZoomFactor = 1;

            // Zooming bug workaround
            // The intented code is in the else clause
#if WINDOWS_PHONE_APP
            _scrollViewer.MinZoomFactor = normalZoomFactor;
            _scrollViewer.MaxZoomFactor = normalZoomFactor;

            await Task.Delay(1);

            _scrollViewer.MinZoomFactor = minZoomFactor;
            _scrollViewer.MaxZoomFactor = maxZoomFactor;
#else
            _scrollViewer.MinZoomFactor = minZoomFactor;
            _scrollViewer.MaxZoomFactor = maxZoomFactor;
            _scrollViewer.ChangeView(null, null, normalZoomFactor, true);
#endif
        }

        private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (Source == null)
            {
                _listView.ItemsSource = _viewModel = null;
                return;
            }

            _listView.ItemsSource = _viewModel = new DjvuDocumentViewModel(Source);
            UpdateZoomConstraints();
        }

        private void OnPageNumberChanged(DependencyPropertyChangedEventArgs e)
        {
            if (Source == null)
                throw new InvalidOperationException("Source is null.");
            if (PageNumber == 0 || PageNumber > Source.PageCount)
                throw new InvalidOperationException("PageNumber is out of range.");

            GoToPage(PageNumber);
        }

        private void GoToPage(uint pageNumber)
        {
            var pageIndex = (int)(pageNumber - 1);
            var page = _viewModel[pageIndex];
            _listView.ScrollIntoView(page, ScrollIntoViewAlignment.Leading);
        }

        private static void SourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (DocumentViewer) d;
            sender.OnSourceChanged(e);
        }

        private static void PageNumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (DocumentViewer) d;
            sender.OnPageNumberChanged(e);
        }
    }
}
