using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
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

        public MainPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            CurrentApp.LicenseInformation.LicenseChanged += LicenseChangedHandler;
            await LoadTrialModeProxyFileAsync();
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
                    trialExpirationTextBlock.Text = string.Format("TRIAL VERSION, {0} DAYS LEFT", Math.Ceiling(trialTimeLeft.TotalDays));
                }
            }
            else
            {
                var dialog = new MessageDialog("Your license has expired. We hope you enjoyed using this application.",
                    "DjVu Reader");
                dialog.Commands.Add(new UICommand("buy", async command =>
                {
                    await CurrentApp.RequestAppPurchaseAsync(false);
                    if (!CurrentApp.LicenseInformation.IsActive)
                        Application.Current.Exit();
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
