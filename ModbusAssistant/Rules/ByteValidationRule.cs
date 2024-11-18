using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusAssistant.Rules
{
    internal class ByteValidationRule : NumericRangeValidationRuleBase
    {
        public override double Min { get; set; } = byte.MinValue;
        public override double Max { get; set; } = byte.MaxValue;
    }
}
