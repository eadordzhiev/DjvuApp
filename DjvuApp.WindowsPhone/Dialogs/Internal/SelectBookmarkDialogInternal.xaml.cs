using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using DjvuApp.Model.Books;

namespace DjvuApp.Dialogs.Internal
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
