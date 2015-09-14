using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DjvuApp.Misc;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class JumpToPageDialogInternal : ContentDialog
    {
        public uint? PageNumber { get; private set; }

        public uint CurrentPageNumber { get; set; }

        public string PageNumberText
        {
            get { return (string)GetValue(PageNumberTextProperty); }
            set { SetValue(PageNumberTextProperty, value); }
        }

        public uint PageCount
        {
            get { return (uint)GetValue(PageCountProperty); }
            set { SetValue(PageCountProperty, value); }
        }

        public static readonly DependencyProperty PageNumberTextProperty =
            DependencyProperty.Register("PageNumberText", typeof(string), typeof(JumpToPageDialogInternal), new PropertyMetadata(null, PageNumberTextChangedCallback));

        public static readonly DependencyProperty PageCountProperty =
            DependencyProperty.Register("PageCount", typeof(uint), typeof(JumpToPageDialogInternal), new PropertyMetadata(0u));

        public JumpToPageDialogInternal()
        {
            this.InitializeComponent();
        }

        private static void PageNumberTextChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (JumpToPageDialogInternal) d;
            sender.OnPageNumberTextChanged(e);
        }

        private void OnPageNumberTextChanged(DependencyPropertyChangedEventArgs e)
        {
            uint pageNumber;
            if (UInt32.TryParse(PageNumberText, out pageNumber) &&
                1 <= pageNumber && pageNumber <= PageCount)
            {
                IsPrimaryButtonEnabled = true;
                PageNumber = pageNumber;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
                PageNumber = null;
            }
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            PageNumberText = CurrentPageNumber.ToString();
            pageNumberTextBox.FocusAndSelectAll();
        }

        private void KeyDownHandler(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Hide();
            }
        }
    }
}
