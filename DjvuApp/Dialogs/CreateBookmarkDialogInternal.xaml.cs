using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace DjvuApp.Dialogs
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

        public static readonly DependencyProperty BookmarkTitleProperty =
            DependencyProperty.Register("BookmarkTitle", typeof(string), typeof(CreateBookmarkDialogInternal), new PropertyMetadata(null, BookmarkTitleChangedCallback));

        public static readonly DependencyProperty CanSaveProperty =
            DependencyProperty.Register("CanSave", typeof(bool), typeof(CreateBookmarkDialogInternal), new PropertyMetadata(false));

        public CreateBookmarkDialogInternal()
        {
            this.InitializeComponent();
            BookmarkTitle = "Unnamed bookmark";
        }

        private static void BookmarkTitleChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (CreateBookmarkDialogInternal) d;

            sender.CanSave = !string.IsNullOrWhiteSpace(sender.BookmarkTitle);
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
            
        }
    }
}
