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
using DjvuApp.Pages;

namespace DjvuApp
{
    public sealed partial class App : Application
    {
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
        
        public static async Task RateApp()
        {
            var uri = new Uri("ms-windows-store:reviewapp?appid=appc6f56627-a976-443c-8531-00a92b42f4e5");
            await Launcher.LaunchUriAsync(uri);
        }
    }
}