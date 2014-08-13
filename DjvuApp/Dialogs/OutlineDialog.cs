using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Model.Outline;

namespace DjvuApp.Dialogs
{
    public sealed class OutlineDialog
    {
        private readonly Outline _outline;

        public OutlineDialog(Outline outline)
        {
            _outline = outline;
        }

        public async Task<uint?> ShowAsync()
        {
            var dialog = new OutlineDialogInternal(_outline);
            await dialog.ShowAsync();
            return dialog.TargetPageNumber;
        }
    }
}
