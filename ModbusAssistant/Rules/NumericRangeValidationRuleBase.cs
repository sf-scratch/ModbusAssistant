using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ModbusAssistant.Rules
{
    public abstract class NumericRangeValidationRuleBase : ValidationRule
    {
        public abstract double Min { get; set; }
        public abstract double Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double number;
            if (!double.TryParse(value.ToString(), out number))
            {
                return new ValidationResult(false, "输入不是有效的数字。");
            }

            if (number < Min || number > Max)
            {
                return new ValidationResult(false, $"输入的数字必须在{Min}和{Max}之间。");
            }

            return ValidationResult.ValidResult;
        }
    }
}
