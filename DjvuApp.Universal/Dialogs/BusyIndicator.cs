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
        private readonly Popup _popup;

        public BusyIndicator()
        {
            _popup = new Popup { IsLightDismissEnabled = false };
        }

        public void Show(string taskDescription)
        {
            if (_popup.IsOpen)
            {
                return;
            }

            var content = new BusyIndicatorInternal
            {
                Width = Window.Current.Bounds.Width,
                Height = Window.Current.Bounds.Height,
                TaskDescription = taskDescription
            };

            _popup.Child = content;
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
            _popup.Child = null;
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            var child = (FrameworkElement) _popup.Child;
            child.Width = e.Size.Width;
            child.Height = e.Size.Height;
        }
    }
}
