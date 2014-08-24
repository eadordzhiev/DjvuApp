using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.ViewModel;

namespace DjvuApp.Pages
{
    public sealed partial class MainPage : Page
    {
        private readonly NavigationHelper _navigationHelper;

        public MainPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(ViewerPage), e.ClickedItem);
        }
    }
}
