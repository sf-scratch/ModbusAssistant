using NModbus;
using NModbus.IO;
using Prism.DryIoc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Resources;

namespace ModbusAssistant.Utils
{
    public class ModbusUtil
    {

        public async static Task<string> WriteSingleCoilAsync(TcpClient client, byte slaveAddress, ushort coilAddress, bool value)
        {
            byte[] coils = BitConverter.GetBytes(coilAddress);
            byte[] datas = new byte[12] 
            { 
                0x00, 0x00,//事务
                0x00, 0x00,//协议
                0x00, 0x06,//长度
                slaveAddress,//单元标识符
                0x05,//功能码
                coils[1],//线圈高
                coils[0],//线圈低
                value ? (byte)0xff : (byte)0x00,//断通标志
                0x00//断通标志
            }; 
            await client.GetStream().WriteAsync(datas, 0, datas.Length);
            await Task.Delay(100);
            return ReceiveToTcp(client);
        }

        private static string ReceiveToTcp(TcpClient client)
        {
            string receivedMessage = string.Empty;
            byte[] buffer = new byte[1024];
            int numberOfBytesRead;
            NetworkStream stream = client.GetStream();
            if (stream.DataAvailable && (numberOfBytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                byte[] receiveData = new byte[numberOfBytesRead];
                Array.Copy(buffer, 0, receiveData, 0, numberOfBytesRead);
                receivedMessage = string.Join(" ", receiveData.Select(b => b.ToString("X2")));
            }
            return receivedMessage;
        }

        public static byte[] UShortArrayToByteArray(ushort[] ushortArray)
        {
            byte[] byteArray = new byte[ushortArray.Length * sizeof(ushort)];
            Buffer.BlockCopy(ushortArray, 0, byteArray, 0, ushortArray.Length * sizeof(ushort));
            return byteArray;
        }
    }
}
