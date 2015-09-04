using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp.Pages
{
    public sealed partial class ViewerPage : Page
    {
        public ViewerViewModel ViewModel { get; } = ServiceLocator.Current.GetInstance<ViewerViewModel>();

        private readonly NavigationHelper _navigationHelper;
        private object _navigationParameter;

        public ViewerPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);

            ViewModel.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);

            _navigationParameter = e.Parameter;
            
            ViewModel.OnNavigatedTo(e);

            if (e.Parameter is IStorageFile)
            {
                notificationPanel.Visibility = Visibility.Visible;
                viewLibraryButton.Visibility = Visibility.Visible;
            }
        }

        private void CloseNotificationButtonClickHandler(object sender, RoutedEventArgs e)
        {
            notificationPanel.Visibility = Visibility.Collapsed;
        }

        private void ViewLibraryButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var rootFrame = (Frame)Window.Current.Content;
            rootFrame.Navigate(typeof(MainPage), null);
            rootFrame.BackStack.Clear();
        }

        private void AddBookButtonClick(object sender, RoutedEventArgs e)
        {
            var rootFrame = (Frame)Window.Current.Content;
            rootFrame.Navigate(typeof(MainPage), _navigationParameter);
            rootFrame.BackStack.Clear();
        }

        private void FindButtonClickHandler(object sender, RoutedEventArgs e)
        {
            searchPanel.Visibility = Visibility.Visible;
            appBar.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_OnQueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs e)
        {
            var query = searchBox.QueryText;

            if (string.IsNullOrWhiteSpace(query))
            {
                query = null;
            }

            readerControl.HighlightSearchMatches(query);
        }

        private async void SearchBox_OnQuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(searchBox.QueryText))
            {
                return;
            }

            searchBox.IsEnabled = false;
            searchProgressBar.Visibility = Visibility.Visible;

            await readerControl.SelectNextSearchMatch();

            searchBox.IsEnabled = true;
            searchProgressBar.Visibility = Visibility.Collapsed;
        }

        private void CloseSearchButtonClickHandler(object sender, RoutedEventArgs e)
        {
            searchPanel.Visibility = Visibility.Collapsed;
            appBar.Visibility = Visibility.Visible;
        }
    }
}
