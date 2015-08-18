using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using DjvuApp.Dialogs.Internal;
using DjvuApp.Djvu;

namespace DjvuApp.Dialogs
{
    public static class OutlineDialog
    {
        public static async Task<uint?> ShowAsync(IReadOnlyList<DjvuOutlineItem> outline)
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("OutlineDialog_Title");

            var head = new DjvuOutlineItem(title, 0, outline);

            var dialog = new OutlineDialogInternal { DataContext = head };
            await dialog.ShowAsync();

            return dialog.TargetPageNumber;
        }
    }
}
