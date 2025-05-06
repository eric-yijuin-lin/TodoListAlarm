using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ToDoListAlram.View.Converters
{
    public class DateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime dueDate)
                return Brushes.White;

            var today = DateTime.Today;
            var daysDiff = (dueDate - today).TotalDays;

            if (daysDiff < 1)
                return Brushes.Red;
            else if (daysDiff < 3)
                return Brushes.Orange;
            else
                return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
