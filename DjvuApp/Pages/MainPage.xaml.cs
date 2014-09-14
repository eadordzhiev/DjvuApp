using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Common;
#if TRIAL_SIMULATION
using CurrentApp = Windows.ApplicationModel.Store.CurrentAppSimulator;
#endif

namespace DjvuApp.Pages
{
    public sealed partial class MainPage : Page
    {
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
            
            await LoadTrialModeProxyFileAsync();
            CurrentApp.LicenseInformation.LicenseChanged += LicenseChangedHandler;
            await Task.Delay(100);
            LicenseChangedHandler();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            CurrentApp.LicenseInformation.LicenseChanged -= LicenseChangedHandler;
            base.OnNavigatingFrom(e);
        }

// ReSharper disable CSharpWarnings::CS1998
        private async Task LoadTrialModeProxyFileAsync()
// ReSharper restore CSharpWarnings::CS1998
        {
#if TRIAL_SIMULATION
            var proxyDataFolder = await Package.Current.InstalledLocation.GetFolderAsync("TrialSimulation");
            var proxyFile = await proxyDataFolder.GetFileAsync("License.xml");
            await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
#endif
        }

        private async void LicenseChangedHandler()
        {
            var licenseInformation = CurrentApp.LicenseInformation;

            buyAppBarButton.Visibility = trialExpirationTextBlock.Visibility =
                    licenseInformation.IsTrial
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (licenseInformation.IsActive)
            {
                if (licenseInformation.IsTrial)
                {
                    var trialTimeLeft = licenseInformation.ExpirationDate - DateTimeOffset.Now;
                    var formatString = _resourceLoader.GetString("TrialNotificationFormat");
                    trialExpirationTextBlock.Text = string.Format(formatString, Math.Ceiling(trialTimeLeft.TotalDays));
                }
            }
            else
            {
                var title = _resourceLoader.GetString("ExpirationDialog_Title");
                var content = _resourceLoader.GetString("ExpirationDialog_Content");
                var buyButtonCaption = _resourceLoader.GetString("ExpirationDialog_BuyButton_Caption");
                var dialog = new MessageDialog(content, title);
                dialog.Commands.Add(new UICommand(buyButtonCaption, async command =>
                {
                    await CurrentApp.RequestAppPurchaseAsync(false);
                    if (!CurrentApp.LicenseInformation.IsActive)
                        LicenseChangedHandler();
                }));
                var result = await dialog.ShowAsync();
                if (result == null)
                    Application.Current.Exit();
            }
        }

        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof (ViewerPage), e.ClickedItem);
        }

        private void AboutButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (AboutPage));
        }

        private async void BuyButtonClickHandler(object sender, RoutedEventArgs e)
        {
            await CurrentApp.RequestAppPurchaseAsync(false);
        }
    }
}
