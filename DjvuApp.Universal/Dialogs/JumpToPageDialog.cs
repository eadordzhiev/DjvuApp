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
        public static async Task<uint?> ShowAsync(uint currentPageNumber, uint pageCount)
        {
            var dialog = new JumpToPageDialogInternal
            {
                CurrentPageNumber = currentPageNumber,
                PageCount = pageCount
            };
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
            return dialog.PageNumber;
        }
    }
}
