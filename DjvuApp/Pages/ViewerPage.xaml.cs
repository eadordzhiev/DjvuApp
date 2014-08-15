using System;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.Model.Books;
using GalaSoft.MvvmLight.Messaging;

namespace DjvuApp.Pages
{
    public sealed partial class ViewerPage : Page
    {
        private readonly NavigationHelper _navigationHelper;
        private IBook _book;

        public ViewerPage()
        {
            this.InitializeComponent();
            this._navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            _book = (IBook) e.Parameter;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
        }
        
        private void AppBar_OnOpened(object sender, object e)
        {
            StatusBar.GetForCurrentView().ShowAsync();
        }

        private void AppBar_OnClosed(object sender, object e)
        {
            StatusBar.GetForCurrentView().HideAsync();
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new LoadedHandledMessage<IBook>(_book));
        }
    }
}
