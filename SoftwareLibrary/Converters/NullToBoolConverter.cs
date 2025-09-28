using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SoftwareLibrary.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNullOrEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
            var inverse = parameter is string p && p.Equals("inverse", StringComparison.OrdinalIgnoreCase);
            var result = !isNullOrEmpty;
            var final = inverse ? !result : result;

            if (targetType == typeof(Visibility) || targetType == typeof(System.Windows.Visibility))
            {
                return final ? Visibility.Visible : Visibility.Collapsed;
            }

            return final;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}