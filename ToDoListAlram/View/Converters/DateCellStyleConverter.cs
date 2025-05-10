using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ToDoListAlram.View.Converters
{
    public class DateCellStyle
    {
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.White;
        public DateCellStyle(DateTime dueDate, bool isWaiting)
        {
            double daysRemaining = (dueDate - DateTime.Now).TotalDays;
            if (daysRemaining < 1)
            {
                if (isWaiting)
                {
                    this.Foreground = Brushes.Red;
                    this.Background = Brushes.White;
                }
                else
                {
                    this.Foreground = Brushes.Black;
                    this.Background = Brushes.Red;
                }
            }
            else if (daysRemaining < 2 && !isWaiting)
            {
                this.Foreground = Brushes.Black;
                this.Background = Brushes.Orange;
            }
        }
    }
    public class DateCellStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not DateTime dueDate)
                return DependencyProperty.UnsetValue;
            if (values[1] is not bool isWaiting)
                return DependencyProperty.UnsetValue;

            var dateCellStyle = new DateCellStyle(dueDate, isWaiting);
            var param = parameter.ToString();
            return param switch
            {
                "Foreground" => dateCellStyle.Foreground,
                "Background" => dateCellStyle.Background,
                _ => DependencyProperty.UnsetValue
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
