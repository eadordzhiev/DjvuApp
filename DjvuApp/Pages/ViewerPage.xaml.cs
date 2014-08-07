using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using DjvuApp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DjvuApp.ViewModel;
using DjvuLibRT;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp
{
    public sealed partial class ViewerPage : Page
    {
        private NavigationHelper navigationHelper;
        public ViewerPage()
        {
            this.InitializeComponent();
            
            this.navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);

            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            if (CurrentBook == null)
                CurrentBook = (Book)e.Parameter;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);

            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);

            SaveCurrentPosition();
        }

        public Book CurrentBook { get; set; }

        public IOutlineItem Outline { get; set; }

        public DjvuDocument CurrentDocument { get; set; }

        private DjvuDocumentViewModel _viewModel;

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            CurrentDocument = new DjvuDocument(CurrentBook.Path);

            var djvuOutline = CurrentDocument.GetBookmarks();
            if (djvuOutline != null)
            {
                Outline = OutlineItem.GetOutline(djvuOutline);
                outlineButton.Visibility = Visibility.Visible;
            }

            await Task.Delay(1);

            _viewModel = new DjvuDocumentViewModel(CurrentDocument);
            listView.ItemsSource = _viewModel;


            GoToPage(CurrentBook.LastOpeningPageNumber);
        }

        private async void OutlineButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var dialog = new OutlineDialog();
            dialog.DataContext = Outline;
            await dialog.ShowAsync();

            var pageNumber = dialog.TargetPageNumber;
            if (pageNumber.HasValue)
                GoToPage(pageNumber.Value);
        }

        private async void JumpToPageButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var dialog = new JumpToPageDialog(CurrentDocument.PageCount);
            await dialog.ShowAsync();

            var pageNumber = dialog.TargetPageNumber;
            if (pageNumber.HasValue)
                GoToPage(pageNumber.Value);
        }

        private async Task SaveCurrentPosition()
        {
            var provider = ServiceLocator.Current.GetInstance<IBookProvider>();
            //await provider.UpdateBookPositionAsync(CurrentBook, documentViewer.CurrentPageNumber);
        }

        private void GoToPage(uint pageNumber)
        {
            if (_viewModel == null)
                return;

            int pageIndex = (int) (pageNumber - 1);
            var page = _viewModel[pageIndex];
            listView.ScrollIntoView(page, ScrollIntoViewAlignment.Leading);
        }

        //private uint GetCurrentPageNumber()
        //{
        //}

        private bool CheckIfElementIsVisible(FrameworkElement element)
        {
            Point point;
            point.X = 0;
            point.Y = 0;
            Rect bounds = Window.Current.Bounds;
            GeneralTransform gt = element.TransformToVisual(Window.Current.Content);
            Point offset = gt.TransformPoint(point);
            bool xResult = offset.X + element.ActualWidth >= 0 && offset.X < bounds.Width;
            bool yResult = offset.Y + element.ActualHeight >= 0 && offset.Y < bounds.Height;
            return xResult && yResult;
        }

        private void AppBar_OnOpened(object sender, object e)
        {
            StatusBar.GetForCurrentView().ShowAsync();
        }

        private void AppBar_OnClosed(object sender, object e)
        {
            StatusBar.GetForCurrentView().HideAsync();
        }
    }
}
