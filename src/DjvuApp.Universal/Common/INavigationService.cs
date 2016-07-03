using System;
using Windows.UI.Xaml.Controls;

namespace DjvuApp.Common
{
    public interface INavigationService : INavigate
    {
        bool CanGoBack { get; }

        void Navigate(Type sourcePageType, object parameter);

        void ClearStack();

        void GoBack();
    }
}