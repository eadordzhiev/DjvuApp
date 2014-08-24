using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Model.Books;

namespace DjvuApp.Dialogs
{
    public static class SelectBookmarkDialog
    {
        public static async Task<IBookmark> ShowAsync(IEnumerable<IBookmark> bookmarks)
        {
            var dialog = new SelectBookmarkDialogInternal(bookmarks);
            await dialog.ShowAsync();

            return dialog.SelectedBookmark;
        }
    }
}
