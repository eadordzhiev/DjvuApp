using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DjvuLibRT;

namespace DjvuApp
{
    public sealed class DocumentViewer : Control
    {
        public DjvuDocument Source
        {
            get { return (DjvuDocument)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public Thickness PageMargin
        {
            get { return (Thickness)GetValue(PageMarginProperty); }
            set { SetValue(PageMarginProperty, value); }
        }

        public double MinZoomFactor
        {
            get { return (double)GetValue(MinZoomFactorProperty); }
            set { SetValue(MinZoomFactorProperty, value); }
        }

        public double MaxZoomFactor
        {
            get { return (double)GetValue(MaxZoomFactorProperty); }
            set { SetValue(MaxZoomFactorProperty, value); }
        }

        public uint CurrentPageNumber
        {
            get { return _currentPageNumber; }
            set
            {
                _currentPageNumber = value;
                CurrentPageNumberChangedCallback(this, null);
            }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(DjvuDocument), typeof(DocumentViewer), new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty PageMarginProperty =
            DependencyProperty.Register("PageMargin", typeof(Thickness), typeof(DocumentViewer), new PropertyMetadata(default(Thickness)));

        public static readonly DependencyProperty MinZoomFactorProperty =
            DependencyProperty.Register("MinZoomFactor", typeof(double), typeof(DocumentViewer), new PropertyMetadata(1D));

        public static readonly DependencyProperty MaxZoomFactorProperty =
            DependencyProperty.Register("MaxZoomFactor", typeof(double), typeof(DocumentViewer), new PropertyMetadata(1D));

        //public static readonly DependencyProperty CurrentPageNumberProperty =
        //    DependencyProperty.Register("CurrentPageNumber", typeof(uint), typeof(DocumentViewer), new PropertyMetadata(1, CurrentPageNumberChangedCallback));

        private ScrollViewer scrollViewer;

        private List<Rect> pageRects;

        private List<ZoomablePagePresenter> pagePresenters;

        private double currentZoomFactor;

        private List<uint> currentVisiblePages = new List<uint>();
        private uint _currentPageNumber;


        public DocumentViewer()
        {
            this.DefaultStyleKey = typeof(DocumentViewer);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            scrollViewer = (ScrollViewer)GetTemplateChild("ScrollViewer");
            scrollViewer.ViewChanged += ViewChangedHandler;
        }

        private void GoToPage(uint pageNumber)
        {
            if (pageNumber < 1 || pageNumber > Source.PageCount)
                throw new ArgumentOutOfRangeException("pageNumber");

            var pageRect = pageRects[(int) (pageNumber - 1)];
            var horizontalOffset = pageRect.X * MinZoomFactor;
            var verticalOffset = pageRect.Y * MinZoomFactor;
            
            scrollViewer.ChangeView(horizontalOffset, verticalOffset, (float) MinZoomFactor, true);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (DocumentViewer)d;

            if (e.NewValue == null && e.OldValue != null)
                sender.Clear();
            else
                sender.OnSourceChangedImpl();
        }

        private ZoomablePagePresenter CreatePresenter(Panel container)
        {
            var presenter = new ZoomablePagePresenter();
            presenter.Margin = PageMargin;
            presenter.HorizontalAlignment = HorizontalAlignment.Center;
            container.Children.Add(presenter);
            pagePresenters.Add(presenter);
            return presenter;
        }

        private void OnSourceChangedImpl()
        {
            var pageInfos = Source.GetPageInfos();

            pageRects = new List<Rect>(pageInfos.Length);
            pagePresenters = new List<ZoomablePagePresenter>(pageInfos.Length);

            double verticalOffset = 0;
            double maxWidth = 0;

            var container = new Canvas();

            foreach (var pageInfo in pageInfos)
            {
                var presenter = CreatePresenter(container);
                presenter.PageTitle = string.Format("{0}|{1}", pageInfo.PageNumber, Source.PageCount);

                var pageRect = new Rect();
                presenter.Width = pageRect.Width = pageInfo.Width;
                presenter.Height = pageRect.Height = pageInfo.Height;
                verticalOffset += PageMargin.Top;
                pageRect.Y = verticalOffset;
                verticalOffset += pageRect.Height + PageMargin.Bottom;

                Canvas.SetLeft(presenter, pageRect.X);
                Canvas.SetTop(presenter, pageRect.Y);

                if (pageRect.Width > maxWidth)
                    maxWidth = pageRect.Width;
                else
                    pageRect.X = (maxWidth - pageRect.Width) / 2;

                pageRects.Add(pageRect);
            }

            container.Width = maxWidth;
            container.Height = verticalOffset;
            scrollViewer.Content = container;
        }

        private async void OnViewChanged(bool isManipulationFinished)
        {
            //if (!isManipulationFinished)
            //    return;

            var viewportRect = GetViewportRect(scrollViewer);
            var visiblePages = new List<uint>();

            for (int i = 0; i < pageRects.Count; i++)
            {
                var pageRect = pageRects[i];

                pageRect.Intersect(viewportRect);
                if (!pageRect.IsEmpty)
                    visiblePages.Add((uint) (i + 1));
            }
            
            if (visiblePages.Any())
            {
                var lowerPageNumber = visiblePages.Min();
                var upperPageNumber = visiblePages.Max();

                // TODO HACK 
                _currentPageNumber = lowerPageNumber + (upperPageNumber - lowerPageNumber) / 2;

                var additionalPageNumber = lowerPageNumber - 1;
                if (additionalPageNumber >= 1)
                    visiblePages.Add(additionalPageNumber);

                additionalPageNumber = upperPageNumber + 1;
                if (additionalPageNumber <= Source.PageCount)
                    visiblePages.Add(additionalPageNumber);
            }

            if (isManipulationFinished)
                currentZoomFactor = MinZoomFactor;//scrollViewer.ZoomFactor;

            foreach (var pageNumber in currentVisiblePages.Except(visiblePages))
            {
                var presenter = pagePresenters[(int) (pageNumber - 1)];
                presenter.Clear();
                Debug.WriteLine("Page {0} has been destroyed", pageNumber);
            }

            foreach (var pageNumber in visiblePages)
            {
                var presenter = pagePresenters[(int) (pageNumber - 1)];

                if (presenter.Source == null)
                {
                    presenter.Source = Source.GetPage(pageNumber);
                    Debug.WriteLine("Page {0} has been created", pageNumber);
                }

                await presenter.Render(currentZoomFactor);
            }

            currentVisiblePages = visiblePages;
        }

        private static Rect GetViewportRect(ScrollViewer viewer)
        {
            var scaleFactor = 1 / viewer.ZoomFactor;

            var verticalOffset = viewer.VerticalOffset * scaleFactor;
            var horizontalOffset = viewer.HorizontalOffset * scaleFactor;
            var width = viewer.ViewportWidth * scaleFactor;
            var height = viewer.ViewportHeight * scaleFactor;

            return new Rect(horizontalOffset, verticalOffset, width, height);
        }

        private void Clear()
        {
            scrollViewer.Content = null;
            pageRects = null;
            pagePresenters = null;
        }

        private void ViewChangedHandler(object sender, ScrollViewerViewChangedEventArgs e)
        {
            OnViewChanged(!e.IsIntermediate);
        }

        private static void CurrentPageNumberChangedCallback(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var sender = (DocumentViewer) s;
            sender.GoToPage(sender.CurrentPageNumber);
        }
    }
}
