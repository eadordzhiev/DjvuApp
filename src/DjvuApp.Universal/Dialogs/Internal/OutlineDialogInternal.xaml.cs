using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using DjvuApp.Djvu;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class OutlineDialogInternal : ContentDialog
    {
        public uint? TargetPageNumber { get; private set; }

        private readonly Stack<DjvuOutlineItem> _history = new Stack<DjvuOutlineItem>();
        
        public OutlineDialogInternal()
        {
            this.InitializeComponent();
        }

        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            var item = (DjvuOutlineItem) e.ClickedItem;
            
            if (item.PageNumber != 0)
            {
                TargetPageNumber = item.PageNumber;
                Hide();
            }
        }

        private void MoreButtonClickHandler(object sender, RoutedEventArgs e)
        {
            var button = (FrameworkElement) sender;
            var item = (DjvuOutlineItem) button.DataContext;

            _history.Push((DjvuOutlineItem) DataContext);
            DataContext = item;
        }

        private void BackButtonClickHandler(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_history.Any())
            {
                args.Cancel = true;
                DataContext = _history.Pop();
            }
        }
    }

}
