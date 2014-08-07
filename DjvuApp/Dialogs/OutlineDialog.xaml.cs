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
    public sealed partial class OutlineDialog : ContentDialog
    {
        public uint? TargetPageNumber { get; set; }

        private OutlineDialog NextDialog { get; set; }

        public OutlineDialog()
        {
            this.InitializeComponent();
        }

        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            var item = (IOutlineItem) e.ClickedItem;
            TargetPageNumber = item.PageNumber;
            Hide();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (ButtonBase) sender;
            var item = (IOutlineItem) button.DataContext;

            if (item.HasItems)
            {
                //DataContext = item;
                Hide();

                var outlineDialog = new OutlineDialog();
                outlineDialog.DataContext = item;
                NextDialog = outlineDialog;
            }
        }

        public new async Task ShowAsync()
        {
            var dialog = this;
            while (dialog != null)
            {
                await ((ContentDialog) dialog).ShowAsync();
                TargetPageNumber = dialog.TargetPageNumber;
                dialog = dialog.NextDialog;
            }
        }
    }

}
