using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DjvuApp.Misc;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class RenameDialogInternal : ContentDialog
    {   
        public string NewName
        {
            get { return (string)GetValue(NewNameProperty); }
            set { SetValue(NewNameProperty, value); }
        }

        public static readonly DependencyProperty NewNameProperty =
            DependencyProperty.Register("NewName", typeof(string), typeof(RenameDialogInternal), new PropertyMetadata(null));
        
        public RenameDialogInternal(string oldName)
        {
            this.InitializeComponent();
            NewName = oldName;
        }

        private void SaveButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Do nothing, the result is handled by ShowAsync()
        }

        private void CancelButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            nameTextBox.FocusAndSelectAll();
        }
    }
}
