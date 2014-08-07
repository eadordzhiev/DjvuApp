using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace DjvuApp
{
    public sealed partial class JumpToPageDialog : ContentDialog
    {
        public uint PageCount { get; private set; }

        public uint? TargetPageNumber { get; private set; }

        public JumpToPageDialog(uint pageCount)
        {
            PageCount = pageCount;
            this.InitializeComponent();

            totalPagesTextBlock.Text = string.Format("The total number of pages is {0}.", PageCount);
        }

        private void PageNumberTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CheckNumberFormat();
        }

        private void CheckNumberFormat()
        {
            uint pageNumber;
            var text = pageNumberTextBox.Text;

            if (uint.TryParse(text, out pageNumber)
                && pageNumber >= 1
                && pageNumber <= PageCount)
            {
                TargetPageNumber = pageNumber;
                IsPrimaryButtonEnabled = true;
            }
            else
            {
                TargetPageNumber = null;
                IsPrimaryButtonEnabled = false;
            }
        }

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            while (!pageNumberTextBox.Focus(FocusState.Programmatic))
            {
                await Task.Delay(1);
            }
            
            CheckNumberFormat();
        }
    }
}
