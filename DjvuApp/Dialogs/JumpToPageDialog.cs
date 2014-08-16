using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuApp.Dialogs
{
    public sealed class JumpToPageDialog
    {
        private readonly uint _pageCount;

        public JumpToPageDialog(uint pageCount)
        {
            _pageCount = pageCount;
        }

        public async Task<uint?> ShowAsync()
        {
            var dialog = new JumpToPageDialogInternal {PageCount = _pageCount};
            await dialog.ShowAsync();
            return dialog.PageNumber;
        }
    }
}
