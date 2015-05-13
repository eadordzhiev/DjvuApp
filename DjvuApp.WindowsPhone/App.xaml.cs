using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Djvu;
using DjvuApp.Misc;
using DjvuApp.Model.Books;
using DjvuApp.Pages;
using Microsoft.Practices.ServiceLocation;
using Microsoft.ApplicationInsights;

namespace DjvuApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Allows tracking page views, exceptions and other telemetry through the Microsoft Application Insights service.
        /// </summary>
        public static TelemetryClient TelemetryClient;

        private TransitionCollection transitions;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            TelemetryClient = new TelemetryClient();

            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

            IocContainer.Init();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Debug.WriteLine("OnLaunched");
            
            if (Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
            
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;
                
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), e != null ? e.Arguments : null))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);

            var file = args.Files.First() as IStorageFile;

            if (file == null)
                throw new Exception();

            OpenFile(file);
        }

        private async void OpenFile(IStorageFile file)
        {
            var tmpFile = await file.CopyAsync(
                destinationFolder: ApplicationData.Current.TemporaryFolder,
                desiredNewName: file.Name,
                option: NameCollisionOption.ReplaceExisting);

            var provider = ServiceLocator.Current.GetInstance<IBookProvider>();

            IBook book;
            try
            {
                book = await provider.AddBookAsync(tmpFile);
            }
            catch (ArgumentException ex)
            {
                ShowDocumentTypeIsNotSupportedMessage();
                TelemetryClient.TrackException(ex);
                return;
            }
            catch (Exception ex)
            {
                ShowDocumentOpeningErrorMessage();
                TelemetryClient.TrackException(ex);
                return;
            }

            try
            {
                await tmpFile.DeleteAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                TelemetryClient.TrackException(ex);
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
            dialog.Commands.Add(new UICommand(okButtonCaption, a => { }));
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
            dialog.Commands.Add(new UICommand(okButtonCaption, a => { }));
            await dialog.ShowAsync();
            Exit();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            var fileOpenArgs = args as FileOpenPickerContinuationEventArgs;
            if (fileOpenArgs != null)
            {
                OnFileOpenPickerContinuationActivated(fileOpenArgs);
            }
        }

        private void OnFileOpenPickerContinuationActivated(FileOpenPickerContinuationEventArgs args)
        {
            var file = args.Files.FirstOrDefault();

            if (file != null)
                OpenFile(file);
        }
    }
}