using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DjvuApp.Misc
{
    public static class FrameworkElementExtensions
    {
        public static IEnumerable<T> GetVisualTreeChildren<T>(this FrameworkElement parent) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, 0) as T;
                if (child == null)
                    continue;

                yield return child;
                foreach (var item in GetVisualTreeChildren<T>(child))
                {
                    yield return item;
                }
            }
        }
    }
}
