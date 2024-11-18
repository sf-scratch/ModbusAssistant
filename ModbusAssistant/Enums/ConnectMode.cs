using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusAssistant.Enums
{
    public enum ConnectMode
    {
        [Description("TCP/IP")]
        TcpIP,
        [Description("Modbus TCP/IP")]
        ModbusTcp
    }
}
