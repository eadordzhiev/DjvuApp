using System;
using Windows.UI.Xaml.Controls;

namespace DjvuApp.Common
{
    public interface INavigationService : INavigate
    {
        void Navigate(Type sourcePageType, object parameter);
        void ClearStack();
        void GoBack();
        bool CanGoBack();
    }
}