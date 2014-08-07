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
using DjvuLibRT;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp
{
    public sealed partial class ViewerPage : Page
    {
        private NavigationHelper navigationHelper;
        private bool isAppBarVisible = true;

        public ViewerPage()
        {
            this.InitializeComponent();
            
            this.navigationHelper = new NavigationHelper(this);
            //this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            //this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            

            navigationHelper.OnNavigatedTo(e);

            if (CurrentBook != null)
                return;

            CurrentBook = (Book)e.Parameter;
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);

            navigationHelper.OnNavigatedFrom(e);

            SaveCurrentPosition();
        }

        public Book CurrentBook { get; set; }

        public IOutlineItem Outline { get; set; }

        public DjvuDocument CurrentDocument { get; set; }

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            CurrentDocument = new DjvuDocument(CurrentBook.Path);

            var djvuOutline = CurrentDocument.GetBookmarks();
            if (djvuOutline != null)
            {
                Outline = OutlineItem.GetOutline(djvuOutline);
                outlineButton.Visibility = Visibility.Visible;
            }

            documentViewer.Source = CurrentDocument;

            await Task.Delay(1);

            documentViewer.CurrentPageNumber = CurrentBook.LastOpeningPageNumber;
        }

        private async void OutlineButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var dialog = new OutlineDialog();
            dialog.DataContext = Outline;
            await dialog.ShowAsync();

            var pageNumber = dialog.TargetPageNumber;
            if (pageNumber.HasValue)
                documentViewer.CurrentPageNumber = pageNumber.Value;
        }

        private async void JumpToPageButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var dialog = new JumpToPageDialog(CurrentDocument.PageCount);
            await dialog.ShowAsync();

            var pageNumber = dialog.TargetPageNumber;
            if (pageNumber.HasValue)
                documentViewer.CurrentPageNumber = pageNumber.Value;
        }

        private async Task SaveCurrentPosition()
        {
            var provider = ServiceLocator.Current.GetInstance<IBookProvider>();
            await provider.UpdateBookPositionAsync(CurrentBook, documentViewer.CurrentPageNumber);
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
