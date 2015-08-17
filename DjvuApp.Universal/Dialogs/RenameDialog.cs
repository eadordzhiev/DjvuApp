using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using DjvuApp.Dialogs.Internal;

namespace DjvuApp.Dialogs
{
    public static class RenameDialog
    {
        public static async Task<string> ShowAsync(string oldName)
        {
            var dialog = new RenameDialogInternal(oldName);
            var result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary ? dialog.NewName : oldName;
        }
    }
}
