using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DjvuApp.Common
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
