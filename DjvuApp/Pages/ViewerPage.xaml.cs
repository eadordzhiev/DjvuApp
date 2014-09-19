using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace DjvuApp.Pages
{
    public sealed partial class ViewerPage : Page
    {
        private readonly NavigationHelper _navigationHelper;
        private IBook _book;

        public ViewerPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);

            Messenger.Default.Send(new OnNavigatedFromMessage(null));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);

            DisplayInformation.AutoRotationPreferences 
                = DisplayOrientations.Portrait 
                | DisplayOrientations.Landscape 
                | DisplayOrientations.LandscapeFlipped;
            
            _book = (IBook) e.Parameter;
            Messenger.Default.Send(new OnNavigatedToMessage(null));
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new LoadedHandledMessage<IBook>(_book));
            GoogleAnalytics.EasyTracker.GetTracker().SendView("ViewerPage");
        }

        private async void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (appBar.Visibility == Visibility.Visible)
            {
                appBar.Visibility = Visibility.Collapsed;
                await StatusBar.GetForCurrentView().HideAsync();
            }
            else
            {
                appBar.Visibility = Visibility.Visible;
                await StatusBar.GetForCurrentView().ShowAsync();
            }
        }

        private void AppBar_OnOpened(object sender, object e)
        {
            appBar.Opacity = 1;
        }

        private void AppBar_OnClosed(object sender, object e)
        {
            appBar.Opacity = 0.7;
        }
    }
}
