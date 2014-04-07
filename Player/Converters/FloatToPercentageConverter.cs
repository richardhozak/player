using System;
using System.Windows.Data;

namespace DriftPlayer.Converters
{
    class FloatToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                double val = Math.Round((double)value, 2) * 100;
                return val + parameter.ToString();
            }
            return value + parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
