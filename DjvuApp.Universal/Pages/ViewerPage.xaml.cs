using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel;
using GalaSoft.MvvmLight.Messaging;
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
                notificationGrid.Visibility = Visibility.Visible;
                viewLibraryButton.Visibility = Visibility.Visible;
            }
        }

        private void CloseNotificationButtonClickHandler(object sender, RoutedEventArgs e)
        {
            notificationGrid.Visibility = Visibility.Collapsed;
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
    }
}
