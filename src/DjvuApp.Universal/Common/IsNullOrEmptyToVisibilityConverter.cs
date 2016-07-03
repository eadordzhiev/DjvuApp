using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DjvuApp.Common
{
    public sealed class IsNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool result;
            var items = value as IEnumerable;
            if (items != null)
            {
                result = items.Cast<object>().Any();
            }
            else
            {
                result = value != null;
            }

            if ((string) parameter == "negate")
            {
                result = !result;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
