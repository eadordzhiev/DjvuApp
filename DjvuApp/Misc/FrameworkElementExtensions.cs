using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DjvuApp.Misc
{
    public static class FrameworkElementExtensions
    {
        public static IEnumerable<T> GetVisualTreeChildren<T>(this FrameworkElement parent) where T : FrameworkElement
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
                Debug.WriteLine("Child {0} of type {1}", child.Name, child.GetType());
                var result = child as T;
                if (result == null)
                    continue;

                yield return result;
                foreach (var item in GetVisualTreeChildren<T>(result))
                {
                    yield return item;
                }
            }
        }
    }
}
