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
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
using DjvuApp.Misc;

namespace DjvuApp.Pages
{
    public sealed partial class MainPage : Page
    {
        private static bool? _hasLicense = null;

        private readonly NavigationHelper _navigationHelper;
        private readonly ResourceLoader _resourceLoader;

        public MainPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _resourceLoader = ResourceLoader.GetForCurrentView();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            if (_hasLicense == null)
            {
                _hasLicense = await LicenseValidator.GetLicenseStatusAsync();
            }
            if (_hasLicense != true)
            {
                var dialog = new MessageDialog("I don't like being pirated...");
                await dialog.ShowAsync();
                Application.Current.Exit();
            }

            ShowRateAppDialog();
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
                var content = _resourceLoader.GetString("RateAppDialog_Content");
                var title = _resourceLoader.GetString("RateAppDialog_Title");
                var rateButtonCaption = _resourceLoader.GetString("RateAppDialog_RateButton_Caption");
                var cancelButtonCaption = _resourceLoader.GetString("RateAppDialog_CancelButton_Caption");
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

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }
        
        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof (ViewerPage), e.ClickedItem);
        }

        private void AboutButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (AboutPage));
        }
    }
}
