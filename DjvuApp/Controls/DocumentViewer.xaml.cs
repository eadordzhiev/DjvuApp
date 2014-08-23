using System;
using System.Collections.Generic;
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
using DjvuApp.ViewModel;
using DjvuLibRT;

namespace DjvuApp.Controls
{
    public sealed partial class DocumentViewer : UserControl
    {
        public DocumentViewer()
        {
            this.InitializeComponent();
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

        private DjvuDocumentViewModel _viewModel;
        private ScrollViewer _scrollViewer;
        private VirtualizingStackPanel _virtualizingStackPanel;

        void _virtualizingStackPanel_CleanUpVirtualizedItemEvent(object sender, CleanUpVirtualizedItemEventArgs e)
        {
            var item = e.Value as DjvuPageViewModel;
            if (item != null)
            {
                item.Dispose();
            }
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
                listView.ItemsSource = _viewModel = null;
                return;
            }

            listView.ItemsSource = _viewModel = new DjvuDocumentViewModel(Source);
            
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
            listView.ScrollIntoView(page, ScrollIntoViewAlignment.Leading);
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

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            SizeChanged += SizeChangedHandler;
        }

        public IEnumerable<FrameworkElement> AllChildren(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, 0);
                if (child is FrameworkElement)
                {
                    yield return child as FrameworkElement;
                    foreach (var item in AllChildren(child))
                    {
                        yield return item;
                    }
                }
            }
        }

        private void LayoutUpdatedHandler(object sender, object e)
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = AllChildren(listView).OfType<ScrollViewer>().FirstOrDefault(control => control.Name == "ScrollViewer");
            }

            if (_virtualizingStackPanel == null)
            {
                _virtualizingStackPanel = (VirtualizingStackPanel) listView.ItemsPanelRoot;
                if (_virtualizingStackPanel != null)
                _virtualizingStackPanel.CleanUpVirtualizedItemEvent += _virtualizingStackPanel_CleanUpVirtualizedItemEvent;
            }
        }
    }
}
