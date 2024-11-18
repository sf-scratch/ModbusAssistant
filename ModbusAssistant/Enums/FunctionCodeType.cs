using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusAssistant.Enums
{
    public enum FunctionCodeType
    {
        [Description("读线圈")]
        ReadCoils = 0x01,
        [Description("读离散量输入")]
        ReadDiscreteInputs = 0x02,
        [Description("读保持寄存器")]
        ReadHoldingRegisters = 0x03,
        [Description("读取输入寄存器")]
        ReadInputRegisters = 0x04,
        [Description("写单个线圈")]
        WriteSingleCoil = 0x05,
        [Description("写单个保持寄存器")]
        WriteSingleRegister = 0x06,
        [Description("写多个线圈")]
        WriteMultipleCoils = 0x0f,
        [Description("写多个保持寄存器")]
        WriteMultipleRegisters = 0x10
    }
}
