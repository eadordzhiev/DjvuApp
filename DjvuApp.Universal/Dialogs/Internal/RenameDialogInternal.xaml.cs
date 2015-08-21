using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DjvuApp.Misc;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class RenameDialogInternal : ContentDialog
    {   
        public string NewName { get; set; }
        
        public RenameDialogInternal()
        {
            this.InitializeComponent();
        }

        private void SaveButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            NewName = nameTextBox.Text;
        }

        private void CancelButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            nameTextBox.FocusAndSelectAll();
        }

        private void NameTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && IsPrimaryButtonEnabled)
            {
                NewName = nameTextBox.Text;
                Hide();
            }
        }

        private void NameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(nameTextBox.Text);
        }
    }
}
