﻿using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using DjvuApp.Model.Outline;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class OutlineDialogInternal : ContentDialog
    {
        public uint? TargetPageNumber { get; private set; }

        public OutlineDialogInternal NextDialog { get; set; }

        public OutlineDialogInternal(object outline)
        {
            this.InitializeComponent();

            DataContext = outline;
        }

        private OutlineDialogInternal(IOutlineItem outline)
        {
            this.InitializeComponent();

            DataContext = outline;
        }

        private void ItemClickHandler(object sender, ItemClickEventArgs e)
        {
            var item = (IOutlineItem) e.ClickedItem;
            if (item.PageNumber != null)
            {
                TargetPageNumber = item.PageNumber;
                Hide();
            }
            else
            {
                ShowNextDialog(item);
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (ButtonBase) sender;
            var item = (IOutlineItem) button.DataContext;

            ShowNextDialog(item);
        }

        private void ShowNextDialog(IOutlineItem item)
        {
            if (item.HasItems)
            {
                Hide();

                var outlineDialog = new OutlineDialogInternal(item);
                NextDialog = outlineDialog;
            }
        }
    }

}