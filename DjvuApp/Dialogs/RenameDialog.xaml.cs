using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DjvuApp.Dialogs
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
