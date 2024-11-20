using ModbusAssistant.Extensions;
using NModbus;
using NModbus.IO;
using Prism.DryIoc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Markup;
using System.Windows.Resources;
using static ImTools.ImMap;

namespace ModbusAssistant.Utils
{
    /// <summary>
    /// 报文的数据格式参考 https://www.modbus.cn/27847.html
    /// </summary>
    public class ModbusUtil
    {
        /// <summary>
        /// 已发送的内容
        /// </summary>
        public static event Action<byte[]> SendDataEvent;

        /// <summary>
        /// 已接收的内容
        /// </summary>
        public static event Action<byte[]> ReceiveDataEvent;

        private static ushort _affairIndex = ushort.MinValue;
        private const int ModbusWritedThenReceiveBytesCount = 12;

        /// <summary>
        /// 读取线圈状态（接收数据错误时返回null）
        /// </summary>
        /// <param name="client"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        /// <returns></returns>
        public async static Task<bool[]> ReadCoilsAsync(TcpClient client, byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            byte[] arrairBytes = GetAffairBytes();
            byte[] startAddressBytes = BitConverter.GetBytes(startAddress);
            byte[] numberOfPointsBytes = BitConverter.GetBytes(numberOfPoints);
            byte[] sendData = new byte[12]
            {
                arrairBytes[1], arrairBytes[0],//事务
                0x00, 0x00,//协议
                0x00, 0x06,//长度
                slaveAddress,//单元标识符
                0x01,//功能码
                startAddressBytes[1], //起始地址高
                startAddressBytes[0], //起始地址低
                numberOfPointsBytes[1],//数量高
                numberOfPointsBytes[0]//数量低
            };
            await client.GetStream().WriteAsync(sendData, 0, sendData.Length);
            SendDataEvent?.Invoke(sendData);
            int coilsBytesCount = numberOfPoints % 8 == 0 ? numberOfPoints / 8 : numberOfPoints / 8 + 1;
            int receiveDataLen = 9 + coilsBytesCount;
            byte[] receiveData = await ReceiveModbusReplayFromTcp(client, 2000, receiveDataLen);
            if (receiveData.Length < receiveDataLen)//接收数据异常
            {
                ReceiveDataEvent?.Invoke(receiveData);
            }
            byte[] tempData = new byte[9];
            Array.Copy(sendData, 0, tempData, 0, 8);
            byte[] receiveLen = BitConverter.GetBytes((ushort)(3 + coilsBytesCount));
            tempData[4] = receiveLen[1];
            tempData[5] = receiveLen[0];
            tempData[8] = (byte)coilsBytesCount;
            byte[] tempData2 = new byte[9];
            Array.Copy(receiveData, 0, tempData2, 0, 9);
            if (tempData.SequenceEqual(tempData2))//接收的返回报文内容检查
            {
                ReceiveDataEvent?.Invoke(receiveData);
                byte[] coilsBytes = new byte[coilsBytesCount];
                Array.Copy(receiveData, 9, coilsBytes, 0, coilsBytesCount);
                bool[] coilsStatus = ByteArrayToBoolArray(coilsBytes);
                bool[] result = new bool[numberOfPoints];
                Array.Copy(coilsStatus, 0, result, 0, numberOfPoints);
                return result;
            }
            else
            {
                return null;
            }
        }

        public async static Task<ushort[]> ReadHoldingRegistersAsync(TcpClient client, byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            byte[] arrairBytes = GetAffairBytes();
            byte[] startAddressBytes = BitConverter.GetBytes(startAddress);
            byte[] numberOfPointsBytes = BitConverter.GetBytes(numberOfPoints);
            byte[] sendData = new byte[12]
            {
                arrairBytes[1], arrairBytes[0],//事务
                0x00, 0x00,//协议
                0x00, 0x06,//长度
                slaveAddress,//单元标识符
                0x03,//功能码
                startAddressBytes[1], //起始地址高
                startAddressBytes[0], //起始地址低
                numberOfPointsBytes[1],//数量高
                numberOfPointsBytes[0]//数量低
            };
            await client.GetStream().WriteAsync(sendData, 0, sendData.Length);
            SendDataEvent?.Invoke(sendData);
            int registerBytesCount = numberOfPoints * 2;
            int receiveDataLen = 9 + registerBytesCount;
            byte[] receiveData = await ReceiveModbusReplayFromTcp(client, 2000, receiveDataLen);
            if (receiveData.Length < receiveDataLen)//接收数据异常
            {
                ReceiveDataEvent?.Invoke(receiveData);
            }
            byte[] tempData = new byte[9];
            Array.Copy(sendData, 0, tempData, 0, 8);
            byte[] receiveLen = BitConverter.GetBytes((ushort)(3 + registerBytesCount));
            tempData[4] = receiveLen[1];
            tempData[5] = receiveLen[0];
            tempData[8] = (byte)registerBytesCount;
            byte[] tempData2 = new byte[9];
            Array.Copy(receiveData, 0, tempData2, 0, 9);
            if (tempData.SequenceEqual(tempData2))//接收的返回报文内容检查
            {
                ReceiveDataEvent?.Invoke(receiveData);
                byte[] registerBytes = new byte[registerBytesCount];
                Array.Copy(receiveData, 9, registerBytes, 0, registerBytesCount);
                return ByteArrayToUShortArray(registerBytes);
            }
            else
            {
                return null;
            }
        }

        public async static Task WriteSingleCoilAsync(TcpClient client, byte slaveAddress, ushort coilAddress, bool value)
        {
            byte[] arrairBytes = GetAffairBytes();
            byte[] coils = BitConverter.GetBytes(coilAddress);
            byte[] sendData = new byte[12] 
            {
                arrairBytes[1], arrairBytes[0],//事务
                0x00, 0x00,//协议
                0x00, 0x06,//长度
                slaveAddress,//单元标识符
                0x05,//功能码
                coils[1], //线圈地址高
                coils[0], //线圈地址低
                value ? (byte)0xff : (byte)0x00,//断通标志
                0x00//断通标志
            }; 
            await client.GetStream().WriteAsync(sendData, 0, sendData.Length);
            SendDataEvent?.Invoke(sendData);
            byte[] receiveData = await ReceiveModbusReplayFromTcp(client, 2000, ModbusWritedThenReceiveBytesCount);
            if (sendData.SequenceEqual(receiveData))
            {
                ReceiveDataEvent?.Invoke(receiveData);
            }
        }
        public async static Task WriteSingleRegisterAsync(TcpClient client, byte slaveAddress, ushort coilAddress, ushort value)
        {
            byte[] arrairBytes = GetAffairBytes();
            byte[] coils = BitConverter.GetBytes(coilAddress);
            byte[] writeData = BitConverter.GetBytes(value);
            byte[] sendData = new byte[12]
            {
                arrairBytes[1], arrairBytes[0],//事务
                0x00, 0x00,//协议
                0x00, 0x06,//长度
                slaveAddress,//单元标识符
                0x06,//功能码
                coils[1], //寄存器地址高
                coils[0], //寄存器地址低
                writeData[1], writeData[0]//写入值
            };
            await client.GetStream().WriteAsync(sendData, 0, sendData.Length);
            SendDataEvent?.Invoke(sendData);
            byte[] receiveData = await ReceiveModbusReplayFromTcp(client, 2000, ModbusWritedThenReceiveBytesCount);
            if (sendData.SequenceEqual(receiveData))
            {
                ReceiveDataEvent?.Invoke(receiveData);
            }
        }

        public async static Task WriteMultipleCoilsAsync(TcpClient client, byte slaveAddress, ushort coilAddress, bool[] data)
        {
            byte[] arrairBytes = GetAffairBytes();
            byte[] writeData = BoolArrayToByteArray(data);
            byte[] len = BitConverter.GetBytes((ushort)(7 + writeData.Length));
            byte[] address = BitConverter.GetBytes(coilAddress);
            byte[] dataCount = BitConverter.GetBytes((ushort)data.Length);
            List<byte> sendData = new List<byte>
            {
                arrairBytes[1], arrairBytes[0],//事务
                0x00, 0x00,//协议
                len[1], len[0],//长度
                slaveAddress,//单元标识符
                0x0f,//功能码
                address[1], address[0],//起始地址
                dataCount[1], dataCount[0],//数量
                (byte)(data.Length % 8 == 0 ? data.Length / 8 : data.Length / 8 + 1)//字节数
            };
            sendData.AddRange(writeData);//写入值
            await client.GetStream().WriteAsync(sendData.ToArray(), 0, sendData.Count);
            SendDataEvent?.Invoke(sendData.ToArray());
            byte[] receiveData = await ReceiveModbusReplayFromTcp(client, 2000, ModbusWritedThenReceiveBytesCount);
            byte[] tempData = new byte[ModbusWritedThenReceiveBytesCount];
            Array.Copy(sendData.ToArray(), 0, tempData, 0, ModbusWritedThenReceiveBytesCount);
            tempData[4] = 0;//返回报文长度的值固定为6
            tempData[5] = 6;
            if (tempData.SequenceEqual(receiveData))//判断返回报文是否正确
            {
                ReceiveDataEvent?.Invoke(receiveData);
            }
        }

        public async static Task WriteMultipleRegistersAsync(TcpClient client, byte slaveAddress, ushort coilAddress, ushort[] data)
        {
            byte[] arrairBytes = GetAffairBytes();
            byte[] writeData = UShortArrayToByteArray(data);
            byte[] len = BitConverter.GetBytes((ushort)(7 + writeData.Length));
            byte[] address = BitConverter.GetBytes(coilAddress);
            byte[] dataCount = BitConverter.GetBytes((ushort)data.Length);
            List<byte> sendData = new List<byte>
            {
                arrairBytes[1], arrairBytes[0],//事务
                0x00, 0x00,//协议
                len[1], len[0],//长度
                slaveAddress,//单元标识符
                0x10,//功能码
                address[1], address[0],//起始地址
                dataCount[1], dataCount[0],//数量
                (byte)(writeData.Length)//字节数
            };
            sendData.AddRange(writeData);//写入值
            await client.GetStream().WriteAsync(sendData.ToArray(), 0, sendData.Count);
            SendDataEvent?.Invoke(sendData.ToArray());
            byte[] receiveData = await ReceiveModbusReplayFromTcp(client, 2000, ModbusWritedThenReceiveBytesCount);
            byte[] tempData = new byte[ModbusWritedThenReceiveBytesCount];
            Array.Copy(sendData.ToArray(), 0, tempData, 0, ModbusWritedThenReceiveBytesCount);
            tempData[4] = 0;//返回报文长度的值固定为6
            tempData[5] = 6;
            if (tempData.SequenceEqual(receiveData))//判断返回报文是否正确
            {
                ReceiveDataEvent?.Invoke(receiveData);
            }
        }

        private async static Task<byte[]> ReceiveModbusReplayFromTcp(TcpClient client, int timeOut, int bytesCount)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = timeOut;
            timer.Elapsed += (sender, e) => 
            {
                cts.Cancel();
            };
            timer.Start();

            List<byte> data = new List<byte>();
            await Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int numberOfBytesRead;
                    NetworkStream stream = client.GetStream();
                    // 循环读取网络流中的数据，直到接收到预期数据量
                    while (!cts.IsCancellationRequested && client.IsOnline() && data.Count < bytesCount)
                    {
                        if (stream.DataAvailable && (numberOfBytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            byte[] receiveData = new byte[numberOfBytesRead];
                            Array.Copy(buffer, 0, receiveData, 0, numberOfBytesRead);
                            data.AddRange(receiveData);
                        }
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return data.ToArray();
        }

        private static string BytesArrayToStr(byte[] data)
        {
            return string.Join(" ", data.Select(b => b.ToString("X2")));
        }

        private static byte[] GetAffairBytes()
        {
            byte[] affairBytes = BitConverter.GetBytes(_affairIndex);
            if (_affairIndex == ushort.MaxValue)
            {
                _affairIndex = ushort.MinValue;
            }
            else
            {
                ++_affairIndex;
            }
            return affairBytes;
        }

        /// <summary>
        /// 将字节数组转换成布尔数组
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static bool[] ByteArrayToBoolArray(byte[] bytes)
        {
            bool[] bools = new bool[bytes.Length * 8];
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                for (int j = 0; j < 8; j++)
                {
                    // 将第j位 (0-7) 转换为bool值
                    bools[i * 8 + j] = (b & (1 << j)) != 0;
                }
            }
            return bools;
        }

        /// <summary>
        /// 将布尔数组转换成字节数组
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static byte[] BoolArrayToByteArray(bool[] value)
        {
            int byteLength = value.Length % 8 == 0 ? value.Length / 8 : value.Length / 8 + 1;

            byte[] result = new byte[byteLength];

            for (int i = 0; i < result.Length; i++)
            {

                int total = value.Length < 8 * (i + 1) ? value.Length - 8 * i : 8;

                for (int j = 0; j < total; j++)
                {
                    result[i] = SetBitValue(result[i], j, value[8 * i + j]);
                }
            }
            return result;
        }

        /// <summary>
        /// 将 某个字节 某个位  置位或者复位
        /// </summary>
        /// <param name="src"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static byte SetBitValue(byte src, int bit, bool value)
        {
            return value ? (byte)(src | (byte)Math.Pow(2, bit)) : (byte)(src & ~(byte)Math.Pow(2, bit));
        }

        /// <summary>
        /// ushort数组转byte数组（大端）
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static byte[] UShortArrayToByteArray(ushort[] array)
        {
            int occupyByteLen = sizeof(ushort);
            byte[] byteArray = new byte[array.Length * occupyByteLen];
            for (int i = 0; i < array.Length; i++)
            {
                byte[] datas = BitConverter.GetBytes(array[i]);
                for (int j = 0; j < occupyByteLen; j++)
                {
                    byteArray[i * occupyByteLen + (occupyByteLen - j - 1)] = datas[j];
                }
            }
            return byteArray;
        }

        /// <summary>
        /// byte数组转ushort数组（大端）
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static ushort[] ByteArrayToUShortArray(byte[] array)
        {
            ushort[] result = new ushort[array.Length / 2];
            for (int i = 0; i + 1 < array.Length; i += 2)
            {
                result[i / 2] = (ushort)((array[i] << 8) | array[i + 1]);
            }
            return result;
        }
    }
}
