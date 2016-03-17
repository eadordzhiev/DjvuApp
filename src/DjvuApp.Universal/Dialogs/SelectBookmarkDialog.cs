using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Dialogs.Internal;
using DjvuApp.Model.Books;

namespace DjvuApp.Dialogs
{
    public static class SelectBookmarkDialog
    {
        public static async Task<IBookmark> ShowAsync(IEnumerable<IBookmark> bookmarks)
        {
            var dialog = new SelectBookmarkDialogInternal(bookmarks);
            var task = dialog.ShowAsync();
            using (DialogManager.GetForCurrentThread().AddPendingDialog(task))
            {
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                }
            }

            return dialog.SelectedBookmark;
        }
    }
}
