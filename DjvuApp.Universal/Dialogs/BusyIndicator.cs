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
            set
            {
                if (_content != null)
                {
                    _content.TaskDescription = value;
                }
            }
        }

        private readonly Popup _popup;
        private BusyIndicatorInternal _content;

        public BusyIndicator()
        {
            _popup = new Popup { IsLightDismissEnabled = false };
        }

        public void Show()
        {
            if (_popup.IsOpen)
            {
                return;
            }

            _content = new BusyIndicatorInternal
            {
                Width = Window.Current.Bounds.Width,
                Height = Window.Current.Bounds.Height
            };

            _popup.Child = _content;
            _popup.IsOpen = true;
            
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        public void Hide()
        {
            if (!_popup.IsOpen)
            {
                return;
            }

            _popup.IsOpen = false;
            Window.Current.SizeChanged -= Current_SizeChanged;

            _popup.Child = null;
            _content = null;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            _content.Width = e.Size.Width;
            _content.Height = e.Size.Height;
        }
    }
}
