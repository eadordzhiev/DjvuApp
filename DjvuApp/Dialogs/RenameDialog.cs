using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DjvuApp.Dialogs
{
    public sealed class RenameDialog
    {
        private readonly string _oldName;

        public RenameDialog(string oldName)
        {
            _oldName = oldName;
        }

        public async Task<string> ShowAsync()
        {
            var dialog = new RenameDialogInternal(_oldName);
            var result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary ? dialog.NewName : _oldName;
        }
    }
}
