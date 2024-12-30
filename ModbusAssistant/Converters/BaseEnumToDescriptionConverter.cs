using ModbusAssistant.Enums;
using ModbusAssistant.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ModbusAssistant.Converters
{
    internal class BaseEnumToDescriptionConverter<T> : BaseValueConverter where T : Enum
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is T mode)
            {
                return mode.GetEnumDescription();
            }
            return "IEnumToDescriptionConverter转换错误";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string mode)
            {
                var fields = typeof(T).GetFields();
                foreach (var field in fields)
                {
                    DescriptionAttribute[] attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
                    if (attribute != null && attribute.Length > 0 && attribute[0].Description == mode)
                    {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return Binding.DoNothing;
        }
    }
}
