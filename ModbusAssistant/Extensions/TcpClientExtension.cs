using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ModbusAssistant.Extensions
{
    public static class TcpClientExtension
    {
        public static bool IsOnline(this TcpClient c)
        {
            return !((c.Client.Poll(10, SelectMode.SelectRead) && (c.Client.Available == 0)) || !c.Client.Connected);
        }
    }
}
