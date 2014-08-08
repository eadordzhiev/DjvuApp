using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using DjvuApp.Model.Outline;

namespace DjvuApp.Dialogs
{
    public sealed partial class OutlineDialog : ContentDialog
    {
        public uint? TargetPageNumber { get; set; }

        private OutlineDialog NextDialog { get; set; }

        public OutlineDialog(IOutlineItem outline)
        {
            this.InitializeComponent();

            DataContext = outline;
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
                Hide();

                var outlineDialog = new OutlineDialog(item);
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
