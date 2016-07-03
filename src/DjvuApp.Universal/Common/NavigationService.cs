using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DjvuApp.Common
{
    public sealed class NavigationService : INavigationService
    {
        private static Frame Frame => (Frame) Window.Current.Content;

        public bool Navigate(Type sourcePageType)
        {
            return Frame.Navigate(sourcePageType);
        }

        public bool CanGoBack => Frame != null && Frame.CanGoBack;

        public void Navigate(Type sourcePageType, object parameter)
        {
            Frame.Navigate(sourcePageType, parameter);
        }

        public void ClearStack()
        {
            Frame.BackStack.Clear();
        }

        public void GoBack()
        {
            if (Frame != null && Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}