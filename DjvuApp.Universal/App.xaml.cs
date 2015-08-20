using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Misc;
using DjvuApp.Model.Books;
using DjvuApp.Pages;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            IocContainer.Init();
        }

        private Frame GetRootFrame()
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootFrame;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.BackgroundColor = (Color)Resources["SystemChromeMediumColor"];
                titleBar.ButtonBackgroundColor = (Color)Resources["SystemChromeMediumColor"];
            }

            return rootFrame;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            var rootFrame = GetRootFrame();
            rootFrame.Navigate(typeof(MainPage), null);

            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);

            var file = (IStorageFile) args.Files.First();

            var rootFrame = GetRootFrame();
            rootFrame.Navigate(typeof(ViewerPage), file);
            rootFrame.BackStack.Clear();

            Window.Current.Activate();
        }

        public async Task OpenFile(IStorageFile file)
        {
            var provider = ServiceLocator.Current.GetInstance<IBookProvider>();

            IBook book;
            try
            {
                book = await provider.AddBookAsync(file);
            }
            catch (NotImplementedException)
            {
                ShowDocumentTypeIsNotSupportedMessage();
                return;
            }
            catch (Exception)
            {
                ShowDocumentOpeningErrorMessage();
                return;
            }

            OnLaunched(null);

            var frame = (Frame) Window.Current.Content;
            frame.Navigate(typeof (ViewerPage), book);
        }

        private async void ShowDocumentOpeningErrorMessage()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("DocumentOpeningErrorDialog_Title");
            var content = resourceLoader.GetString("DocumentOpeningErrorDialog_Content");
            var okButtonCaption = resourceLoader.GetString("DocumentOpeningErrorDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(okButtonCaption));
            await dialog.ShowAsync();
            Exit();
        }

        private async void ShowDocumentTypeIsNotSupportedMessage()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("DocumentTypeIsNotSupportedDialog_Title");
            var content = resourceLoader.GetString("DocumentTypeIsNotSupportedDialog_Content");
            var okButtonCaption = resourceLoader.GetString("DocumentTypeIsNotSupportedDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(okButtonCaption));
            await dialog.ShowAsync();
            Exit();
        }
        
        public static async Task RateApp()
        {
            var uri = new Uri("ms-windows-store:reviewapp?appid=appc6f56627-a976-443c-8531-00a92b42f4e5");
            await Launcher.LaunchUriAsync(uri);
        }
    }
}