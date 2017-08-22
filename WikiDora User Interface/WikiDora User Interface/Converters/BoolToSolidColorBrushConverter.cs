using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WikiDoraUserInterface.Converters {

    [ValueConversion(typeof(bool?), typeof(SolidColorBrush))]
    public class BoolToSolidColorBrushConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool? like = (bool?) value;
            Color brushColor;
            if (like == null) brushColor = Colors.Gray;
            else brushColor = like.Value ? Colors.LightGreen : Colors.Red;
            return new SolidColorBrush(brushColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException("cannot convert back");
        }
    }
}
