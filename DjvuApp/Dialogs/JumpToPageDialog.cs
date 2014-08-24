using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Dialogs.Internal;

namespace DjvuApp.Dialogs
{
    public static class JumpToPageDialog
    {
        public static async Task<uint?> ShowAsync(uint pageCount)
        {
            var dialog = new JumpToPageDialogInternal {PageCount = pageCount};
            await dialog.ShowAsync();
            return dialog.PageNumber;
        }
    }
}
