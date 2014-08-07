using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace DjvuApp
{
    public sealed partial class RenameDialog : ContentDialog
    {
        public RenameDialog(string oldName)
        {
            this.InitializeComponent();
            nameTextBox.Text = oldName;
        }

        public new async Task<string> ShowAsync()
        {
            var result = await base.ShowAsync();
            return result == ContentDialogResult.Primary ? nameTextBox.Text : null;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private async void RenameDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            while (!nameTextBox.Focus(FocusState.Programmatic))
            {
                await Task.Delay(1);
            }
        }
    }
}
