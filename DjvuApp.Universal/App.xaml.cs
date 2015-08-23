using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Misc;
using DjvuApp.Pages;

namespace DjvuApp
{
    public sealed partial class App : Application
    {
        private static readonly List<WeakReference<IAsyncInfo>> _pendingDialogs = new List<WeakReference<IAsyncInfo>>(); 

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
                rootFrame.Navigated += OnNavigated;
                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Background = (Brush) Resources["ApplicationPageBackgroundThemeBrush"];
                 
                Window.Current.Content = rootFrame;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                titleBar.BackgroundColor = (Color)Resources["SystemChromeMediumColor"];
                titleBar.ForegroundColor = (Color)Resources["SystemBaseHighColor"];

                titleBar.ButtonBackgroundColor = (Color)Resources["SystemChromeMediumColor"];
                titleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];

                titleBar.ButtonHoverBackgroundColor = (Color)Resources["SystemChromeLowColor"];
                titleBar.ButtonHoverForegroundColor = (Color)Resources["SystemBaseHighColor"];

                titleBar.ButtonPressedBackgroundColor = (Color)Resources["SystemChromeHighColor"];
                titleBar.ButtonPressedForegroundColor = (Color)Resources["SystemBaseHighColor"];

                titleBar.ButtonInactiveBackgroundColor = (Color)Resources["SystemChromeMediumColor"];
                //titleBar.ButtonInactiveForegroundColor = Colors.Gray;

                titleBar.InactiveBackgroundColor = (Color)Resources["SystemChromeMediumColor"];
                //titleBar.InactiveForegroundColor = Colors.Gray;
            }

            return rootFrame;
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            DismissPendingDialogs();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            var rootFrame = GetRootFrame();
            rootFrame.Navigate(typeof(MainPage), null);
            rootFrame.BackStack.Clear();

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

            DismissPendingDialogs();

            Window.Current.Activate();
        }

        private class PendingDialogAwaiter : IDisposable
        {
            private readonly WeakReference<IAsyncInfo> _reference;

            public PendingDialogAwaiter(WeakReference<IAsyncInfo> reference)
            {
                _reference = reference;
            }

            public void Dispose()
            {
                _pendingDialogs.Remove(_reference);
            }
        }

        public static IDisposable AddPendingDialog(IAsyncInfo asyncInfo)
        {
            var weakReference = new WeakReference<IAsyncInfo>(asyncInfo);
            _pendingDialogs.Add(weakReference);

            return new PendingDialogAwaiter(weakReference);
        }

        private static void DismissPendingDialogs()
        {
            foreach (var weakReference in _pendingDialogs)
            {
                IAsyncInfo asyncInfo;
                if (weakReference.TryGetTarget(out asyncInfo))
                {
                    asyncInfo.Cancel();
                }
            }

            _pendingDialogs.Clear();
        }
        
        public static async Task RateApp()
        {
            var uri = new Uri("ms-windows-store:reviewapp?appid=c6f56627-a976-443c-8531-00a92b42f4e5");
            await Launcher.LaunchUriAsync(uri);
        }
    }
}