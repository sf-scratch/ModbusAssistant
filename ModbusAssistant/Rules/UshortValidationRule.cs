using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusAssistant.Rules
{
    internal class UshortValidationRule : NumericRangeValidationRuleBase
    {
        public override double Min { get; set; } = ushort.MinValue;
        public override double Max { get; set; } = ushort.MaxValue;
    }
}
