using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using DjvuApp.Dialogs.Internal;

namespace DjvuApp.Dialogs
{
    public class BusyIndicator
    {
        public string TaskDescription
        {
            set { _content.TaskDescription = value; }
        }

        private readonly Popup _popup;
        private readonly BusyIndicatorInternal _content;

        public BusyIndicator()
        {
            _popup = new Popup { IsLightDismissEnabled = false };
            _content = new BusyIndicatorInternal();
            _popup.Child = _content;
        }

        public void Show()
        {
            if (_popup.IsOpen)
            {
                return;
            }

            UpdateSize();
            
            _popup.IsOpen = true;
            Window.Current.SizeChanged += SizeChangedHandler;

            _content.OnOpen();
        }

        public async void Hide()
        {
            if (!_popup.IsOpen)
            {
                return;
            }

            await _content.OnClose();

            _popup.IsOpen = false;
            Window.Current.SizeChanged -= SizeChangedHandler;
        }

        private void UpdateSize()
        {
            _content.Width = Window.Current.Bounds.Width;
            _content.Height = Window.Current.Bounds.Height;
        }

        private void SizeChangedHandler(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            UpdateSize();
        }
    }
}
