using System;
using System.Globalization;
using System.Windows.Data;

namespace lanpingcj.Views.Pages
{
    public class BooleanToOnOffConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "开" : "关";
            return "关";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s == "开";
            return false;
        }
    }
}