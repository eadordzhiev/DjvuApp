using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        public DocumentViewer()
        {
            this.DefaultStyleKey = typeof(DocumentViewer);
        }

        protected override void OnApplyTemplate()
        {
            _listView = (ListView) GetTemplateChild("listView");
        }

        private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (Source == null)
            {
                _listView.ItemsSource = _viewModel = null;
                return;
            }

            _listView.ItemsSource = _viewModel = new DjvuDocumentViewModel(Source);
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
