using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.Misc;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp.Pages
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = ServiceLocator.Current.GetInstance<MainViewModel>();

        private readonly NavigationHelper _navigationHelper;

        public MainPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);

            ViewModel.OnNavigatedTo(e);
            ShowRateAppDialog();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        private async void ShowRateAppDialog()
        {
            const string key = "RateDialog_AppLaunchCount";
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(key))
            {
                settings.Values[key] = 0;
            }

            var count = (int) settings.Values[key];
            
            settings.Values[key] = count + 1;
            count++;

            if (count == 5 || count == 10 || count == 15)
            {
                var resourceLoader = ResourceLoader.GetForCurrentView();
                var content = resourceLoader.GetString("RateAppDialog_Content");
                var title = resourceLoader.GetString("RateAppDialog_Title");
                var rateButtonCaption = resourceLoader.GetString("RateAppDialog_RateButton_Caption");
                var cancelButtonCaption = resourceLoader.GetString("RateAppDialog_CancelButton_Caption");
                var dialog = new MessageDialog(content, title);

                dialog.Commands.Add(new UICommand(rateButtonCaption, async command =>
                {
                    settings.Values[key] = 1000;
                    await App.RateApp();
                }));
                dialog.Commands.Add(new UICommand(cancelButtonCaption));
                dialog.CancelCommandIndex = unchecked((uint) -1);

                await dialog.ShowAsync();
            }
        }
        
        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof (ViewerPage), e.ClickedItem);
        }

        private void AboutButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }
    }
}
