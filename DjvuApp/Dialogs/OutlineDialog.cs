using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Dialogs.Internal;
using DjvuApp.Model.Outline;

namespace DjvuApp.Dialogs
{
    public static class OutlineDialog
    {
        public static async Task<uint?> ShowAsync(Outline outline)
        {
            var dialog = new OutlineDialogInternal(outline);
            var history = new Stack<OutlineDialogInternal>();

            while (dialog != null)
            {
                await dialog.ShowAsync();

                if (dialog.TargetPageNumber != null)
                {
                    return dialog.TargetPageNumber;
                }

                var nextDialog = dialog.NextDialog;
                if (nextDialog != null)
                {
                    dialog.NextDialog = null;
                    history.Push(dialog);
                    dialog = nextDialog;
                }
                else
                {
                    dialog = history.Count > 0 ? history.Pop() : null;
                }
            }

            return null;
        }
    }
}
