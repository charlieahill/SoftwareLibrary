using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SoftwareLibrary.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            System.Windows.Media.Color c;
            switch (status)
            {
                case "In development":
                    c = System.Windows.Media.Color.FromRgb(0xFF, 0xA5, 0x00); // orange
                    break;
                case "In testing":
                    c = System.Windows.Media.Color.FromRgb(0xFF, 0xD7, 0x00); // gold/yellow
                    break;
                case "Deployed":
                    c = System.Windows.Media.Color.FromRgb(0x32, 0xCD, 0x32); // limegreen
                    break;
                case "Archived":
                    c = System.Windows.Media.Color.FromRgb(0x80, 0x80, 0x80); // gray
                    break;
                default:
                    c = System.Windows.Media.Colors.Gray;
                    break;
            }

            var brush = new SolidColorBrush(c);
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
