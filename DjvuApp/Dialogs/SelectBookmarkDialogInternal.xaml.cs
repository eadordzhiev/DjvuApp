using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using DjvuApp.Model.Books;

namespace DjvuApp.Dialogs
{
    public sealed partial class SelectBookmarkDialogInternal : ContentDialog
    {
        public IEnumerable<IBookmark> Items { get; private set; }

        public IBookmark SelectedBookmark { get; private set; }

        public SelectBookmarkDialogInternal(IEnumerable<IBookmark> items)
        {
            Items = items;
            this.InitializeComponent();
        }

        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            SelectedBookmark = (IBookmark) e.ClickedItem;
            Hide();
        }
    }
}
