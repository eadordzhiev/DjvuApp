using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Djvu;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace DjvuApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DjvuDocument document;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (document != null)
                return;

            document = await DjvuDocument.LoadAsync("djvu3spec.djvu");
            //document = await DjvuDocument.LoadAsync("strategikon.djvu");
            PageViewControl.Renderer = new Renderer();

            reader.Source = document;
        }
    }
}
