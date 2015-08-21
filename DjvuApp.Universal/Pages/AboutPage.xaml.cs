using Windows.ApplicationModel.Email;
using Windows.System;
using DjvuApp.Common;
using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DjvuApp.Pages
{
    public sealed partial class AboutPage : Page
    {
        private readonly NavigationHelper _navigationHelper;

        public AboutPage()
        {
            this.InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);

            var version = Package.Current.Id.Version;
            versionTextBlock.Text = $@"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}
By Eldar Dordzhiev
From Russia with love :)";
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        private async void ContactMeButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var recipient = new EmailRecipient("djvureaderwp@gmail.com", "Djvu Reader developer");
            var message = new EmailMessage();
            message.To.Add(recipient);
            message.Subject = "DjVu Reader";
            await EmailManager.ShowComposeNewEmailAsync(message);
        }

        private async void ShowMyAppsButtonClickHandler(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store:Publisher?name=Eldar%20Dordzhiev"));
        }

        private async void RateButtonClickHandler(object sender, RoutedEventArgs e)
        {
            await App.RateApp();
        }
    }
}
