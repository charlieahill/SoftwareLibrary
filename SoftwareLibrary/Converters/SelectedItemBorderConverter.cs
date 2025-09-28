using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace SoftwareLibrary.Converters
{
    public class SelectedItemBorderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var current = values.Length > 0 ? values[0] : null;
            var selected = values.Length > 1 ? values[1] : null;
            var isSelected = ReferenceEquals(current, selected) || (current != null && current.Equals(selected));

            if (targetType == typeof(System.Windows.Media.Brush) || targetType == typeof(System.Windows.Media.SolidColorBrush))
            {
                return isSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x90, 0xFF)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            }

            if (targetType == typeof(Thickness))
            {
                return isSelected ? new Thickness(2) : new Thickness(1);
            }

            // fallback to Brush
            return isSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x90, 0xFF)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
