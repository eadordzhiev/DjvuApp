using System;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using DjvuApp.Model.Outline;
using DjvuApp.ViewModel;
using DjvuLibRT;

namespace DjvuApp.Pages
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
        }

        private async void OutlineButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var dialog = new OutlineDialog(Outline);
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

        private void GoToPage(uint pageNumber)
        {
            if (_viewModel == null)
                return;

            int pageIndex = (int) (pageNumber - 1);
            var page = _viewModel[pageIndex];
            listView.ScrollIntoView(page, ScrollIntoViewAlignment.Leading);
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
