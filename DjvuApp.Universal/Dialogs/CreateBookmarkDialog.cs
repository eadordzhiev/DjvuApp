using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using DjvuApp.Dialogs.Internal;

namespace DjvuApp.Dialogs
{
    public static class CreateBookmarkDialog
    {
        public static async Task<string> ShowAsync()
        {
            var dialog = new CreateBookmarkDialogInternal();
            var task = dialog.ShowAsync();
            using (App.AddPendingDialog(task))
            {
                try
                {
                    return await task == ContentDialogResult.Primary ? dialog.BookmarkTitle : null;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }
    }
}
