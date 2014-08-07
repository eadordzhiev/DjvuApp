using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using DjvuApp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DjvuApp.ViewModel;
using DjvuLibRT;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp
{
    public sealed partial class ViewerPage : Page
    {
        private NavigationHelper navigationHelper;
        public ViewerPage()
        {
            this.InitializeComponent();
            
            this.navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);

            Book = (Book)e.Parameter;
        }

        public Book Book { get; set; }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }
        
        private async void ViewerPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            var document = new DjvuDocument(Book.Path);
            await Task.Delay(1);
            listView.ItemsSource = new DjvuDocumentViewModel(document); 
        }
    }
}
