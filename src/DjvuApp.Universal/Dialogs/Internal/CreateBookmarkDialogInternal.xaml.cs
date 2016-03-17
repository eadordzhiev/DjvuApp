using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class CreateBookmarkDialogInternal : ContentDialog
    {
        public bool CanSave
        {
            get { return (bool)GetValue(CanSaveProperty); }
            set { SetValue(CanSaveProperty, value); }
        }

        public string BookmarkTitle
        {
            get { return (string)GetValue(BookmarkTitleProperty); }
            set { SetValue(BookmarkTitleProperty, value); }
        }

        public bool IsSaved { get; set; }

        public static readonly DependencyProperty BookmarkTitleProperty =
            DependencyProperty.Register("BookmarkTitle", typeof(string), typeof(CreateBookmarkDialogInternal), new PropertyMetadata(null, BookmarkTitleChangedCallback));

        public static readonly DependencyProperty CanSaveProperty =
            DependencyProperty.Register("CanSave", typeof(bool), typeof(CreateBookmarkDialogInternal), new PropertyMetadata(false));

        public CreateBookmarkDialogInternal()
        {
            this.InitializeComponent();
            BookmarkTitle = "Bookmark 1";
        }

        private static void BookmarkTitleChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (CreateBookmarkDialogInternal) d;

            sender.CanSave = !string.IsNullOrWhiteSpace(sender.BookmarkTitle);
        }

        private void SaveButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsSaved = true;
        }

        private void CancelButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            nameTextBox.Focus(FocusState.Programmatic);
        }

        private void NameTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && CanSave)
            {
                IsSaved = true;
                Hide();
            }
        }
    }
}
