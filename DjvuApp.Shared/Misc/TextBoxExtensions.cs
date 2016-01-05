using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DjvuApp.Misc
{
    public static class TextBoxExtensions
    {
        public static async void FocusAndSelectAll(this TextBox textBox)
        {
            await textBox.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Focus(FocusState.Programmatic);
                textBox.SelectAll();
            });
        }
    }
}
