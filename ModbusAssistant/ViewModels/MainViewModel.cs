using ImTools;
using Microsoft.Expression.Interactivity.Media;
using ModbusAssistant.Enums;
using ModbusAssistant.Extensions;
using ModbusAssistant.Models;
using ModbusAssistant.Utils;
using NModbus;
using NModbus.Extensions.Enron;
using Prism.Commands;
using Prism.DryIoc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace ModbusAssistant.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public DelegateCommand ConnectCommand { get; set; }
        public DelegateCommand SendCommand { get; set; }
        public ObservableCollection<string> MsgList { get; set; }
        public ObservableCollection<string> ConnectList { get; set; }
        public ObservableCollection<string> FunctionCodeList { get; set; }
        private TcpClient _tcpIpClient;
        private TcpClient _modbusTcpClient;
        private ModbusFactory _factory;
        private IModbusMaster _master;
        private CancellationTokenSource _receiveToTcpIpCTS;

        private ConnectMode _selectConnectMode;

        public ConnectMode SelectConnectMode
        {
            get { return _selectConnectMode; }
            set
            {
                _selectConnectMode = value;
                RaisePropertyChanged();
            }
        }

        private string _IpAddress;

        public string IpAddress
        {
            get { return _IpAddress; }
            set
            {
                _IpAddress = value;
                RaisePropertyChanged();
            }
        }

        private string _port;

        public string Port
        {
            get { return _port; }
            set
            {
                _port = value;
                RaisePropertyChanged();
            }
        }

        private string _timeout;

        public string Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                RaisePropertyChanged();
            }
        }

        private string _sendText;

        public string SendText
        {
            get { return _sendText; }
            set
            {
                _sendText = value;
                RaisePropertyChanged();
            }
        }

        private bool _isConnect;

        public bool IsConnect
        {
            get { return _isConnect; }
            set
            {
                _isConnect = value;
                RaisePropertyChanged();
            }
        }

        private bool _isConnectEnable;

        public bool IsConnectEnable
        {
            get { return _isConnectEnable; }
            set
            {
                _isConnectEnable = value;
                RaisePropertyChanged();
            }
        }

        private SendType _selectSendType;

        public SendType SelectSendType
        {
            get { return _selectSendType; }
            set
            {
                _selectSendType = value;
                RaisePropertyChanged();
            }
        }

        private ModbusDefinition _definition;

        public ModbusDefinition Definition
        {
            get { return _definition; }
            set { _definition = value; }
        }


        public MainViewModel()
        {
            ConnectCommand = new DelegateCommand(Connect);
            SendCommand = new DelegateCommand(Send);
            _factory = new ModbusFactory();
            ConnectList = new ObservableCollection<string>();
            foreach (ConnectMode status in Enum.GetValues(typeof(ConnectMode)))
            {
                ConnectList.Add(status.GetEnumDescription());
            }
            FunctionCodeList = new ObservableCollection<string>();
            foreach (FunctionCodeType type in Enum.GetValues(typeof(FunctionCodeType)))
            {
                FunctionCodeList.Add(type.GetEnumDescription());
            }
            MsgList = new ObservableCollection<string>();
            _isConnect = false;
            _isConnectEnable = true;
            _selectSendType = SendType.HEX;
            _sendText = string.Empty;
            _IpAddress = "127.0.0.1";
            _port = "502";
            _definition = new ModbusDefinition();
            _definition.SlaveID = 110;
            _definition.FunctionCode = FunctionCodeType.WriteSingleCoil;
            _definition.Address = 100;
            _definition.Quantity = 10;
            _definition.ScanRate = 1000;
            MsgList.Add("启动调试助手成功！");
        }

        private void Send()
        {
            if (string.IsNullOrEmpty(_sendText.Trim()) &&
                Definition.FunctionCode == FunctionCodeType.WriteSingleCoil &&
                Definition.FunctionCode == FunctionCodeType.WriteMultipleCoils &&
                Definition.FunctionCode == FunctionCodeType.WriteSingleRegister &&
                Definition.FunctionCode == FunctionCodeType.WriteMultipleRegisters
                )
            {
                MsgList.Add("发送信息为空");
                return;
            }
            if (SelectConnectMode == ConnectMode.TcpIP)
            {
                SendToTcpIp(_sendText);
            }
            else if (SelectConnectMode == ConnectMode.ModbusTcp)
            {
                SendToModbusTcp(_sendText);
            }
        }

        private void SendToTcpIp(string sendText)
        {
            if (_tcpIpClient == null)
            {
                MsgList.Add("未创建连接");
                return;
            }
            string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                if (SelectSendType == SendType.ASCII)
                {
                    byte[] datas = Encoding.ASCII.GetBytes(sendText);
                    _tcpIpClient.GetStream().WriteAsync(datas, 0, datas.Length);
                }
                else if (SelectSendType == SendType.HEX)
                {
                    byte[] sendHexDatas = sendText.Trim().Split(' ').Select(p => Convert.ToByte(p, 16)).ToArray();
                    _tcpIpClient.GetStream().WriteAsync(sendHexDatas, 0, sendHexDatas.Length);
                }
                this.MsgList.Add(string.Format("发送 - [{0}] [{1}] {2}", nowStr, SelectSendType.ToString(), sendText));
            }
            catch (Exception ex)
            {
                this.MsgList.Add(string.Format("发送失败 - [{0}] {1}", nowStr, ex.Message));
            }
        }

        private void ReceiveToTcp()
        {
            Task.Run(() =>
            {
                try
                {
                    // 创建一个缓冲区用于接收数据
                    byte[] buffer = new byte[1024];
                    int numberOfBytesRead;
                    NetworkStream stream = _tcpIpClient.GetStream();
                    // 循环读取网络流中的数据，直到没有更多数据
                    while (!_receiveToTcpIpCTS.IsCancellationRequested && _tcpIpClient.IsOnline())
                    {
                        if (stream.DataAvailable && (numberOfBytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            // 将接收到的字节数据转换为字符串
                            //string receivedMessage = Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead);
                            // 将接收到的字节数据以十六进制显示
                            byte[] receiveData = new byte[numberOfBytesRead];
                            Array.Copy(buffer, 0, receiveData, 0, numberOfBytesRead);
                            string receivedMessage = string.Join(" ", receiveData.Select(b => b.ToString("X2")));
                            // 在UI线程上安全地更新文本框以显示接收到的消息
                            // 这是必要的，因为ReceiveMessages不在UI线程上运行
                            PrismApplication.Current.Dispatcher.Invoke(() =>
                            {
                                MsgList.Add(string.Format("接收 - [{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), receivedMessage));
                            });
                        }
                    }
                    PrismApplication.Current.Dispatcher.Invoke(() =>
                    {
                        IsConnect = false;
                        MsgList.Add("连接断开");
                    });
                }
                catch (ObjectDisposedException)
                {
                    PrismApplication.Current.Dispatcher.Invoke(() =>
                    {
                        IsConnect = false;
                        MsgList.Add("连接断开");
                    });
                }
                catch (Exception ex)
                {
                    PrismApplication.Current.Dispatcher.Invoke(() =>
                    {
                        // 如果在接收数据过程中发生异常（如连接断开），显示错误消息
                        MsgList.Add($"接收消息时出错: {ex.Message}");
                        IsConnect = false;
                        IsConnectEnable = true;
                    });
                }
            });
        }

        private async void SendToModbusTcp(string sendText)
        {
            if (_master == null)
            {
                MsgList.Add("未创建连接");
                return;
            }
            try
            {
                switch (Definition.FunctionCode)
                {
                    case FunctionCodeType.ReadCoils:
                        //_master.ReadCoils(Definition.SlaveID, Definition.Address, Definition.Quantity);
                        bool[] coilsStatus = await ModbusUtil.ReadCoilsAsync(_modbusTcpClient, Definition.SlaveID, Definition.Address, Definition.Quantity);
                        if (coilsStatus != null)
                        {
                            MsgList.Add(string.Join(" ", coilsStatus.Select((p) => p ? 1 : 0)));
                        }
                        else
                        {
                            MsgList.Add("数据异常");
                        }
                        break;
                    case FunctionCodeType.ReadDiscreteInputs:
                        break;
                    case FunctionCodeType.ReadHoldingRegisters:
                        //_master.ReadHoldingRegistersAsync(Definition.SlaveID, Definition.Address, Definition.Quantity);
                        ushort[] registerData = await ModbusUtil.ReadHoldingRegistersAsync(_modbusTcpClient, Definition.SlaveID, Definition.Address, Definition.Quantity);
                        if (registerData != null)
                        {
                            MsgList.Add(string.Join(" ", registerData));
                        }
                        else
                        {
                            MsgList.Add("数据异常");
                        }
                        break;
                    case FunctionCodeType.ReadInputRegisters:
                        break;
                    case FunctionCodeType.WriteSingleCoil:
                        //await _master.WriteSingleCoilAsync(Definition.SlaveID, Definition.Address, sendText == "1");
                        await ModbusUtil.WriteSingleCoilAsync(_modbusTcpClient, Definition.SlaveID, Definition.Address, sendText == "1");
                        break;
                    case FunctionCodeType.WriteSingleRegister:
                        ushort ushortData = Convert.ToUInt16(sendText, 16);
                        //await _master.WriteSingleRegisterAsync(Definition.SlaveID, Definition.Address, ushortData);
                        await ModbusUtil.WriteSingleRegisterAsync(_modbusTcpClient, Definition.SlaveID, Definition.Address, ushortData);
                        break;
                    case FunctionCodeType.WriteMultipleCoils:
                        bool[] sendDatas = sendText.Split(' ').Select(p => p == "1").ToArray();
                        //await _master.WriteMultipleCoilsAsync(Definition.SlaveID, Definition.Address, sendDatas);
                        await ModbusUtil.WriteMultipleCoilsAsync(_modbusTcpClient, Definition.SlaveID, Definition.Address, sendDatas);
                        break;
                    case FunctionCodeType.WriteMultipleRegisters:
                        ushort[] ushortDatas = sendText.Trim().Split(' ').Select(p => Convert.ToUInt16(p, 16)).ToArray();
                        //await _master.WriteMultipleRegistersAsync(Definition.SlaveID, Definition.Address, ushortDatas);
                        await ModbusUtil.WriteMultipleRegistersAsync(_modbusTcpClient, Definition.SlaveID, Definition.Address, ushortDatas);
                        break;
                    default
                        : break;
                }
            }
            catch (Exception ex)
            {
                MsgList.Add(ex.Message);
            }
        }

        private async void Connect()
        {
            if (_isConnect)//触发ToggleButton
            {
                int port;
                if (!int.TryParse(_port, out port))
                {
                    MsgList.Add("端口格式错误");
                    return;
                }
                if (SelectConnectMode == ConnectMode.ModbusTcp)
                {
                    await ConnectToModbusTcpAsync(_IpAddress, port);
                }
                else if (SelectConnectMode == ConnectMode.TcpIP)
                {
                    await ConnectToTcpIPAsync(_IpAddress, port);
                }
            }
            else
            {
                _receiveToTcpIpCTS?.Cancel();
                _master?.Dispose();
                _master = null;
                _tcpIpClient?.Dispose();
                _tcpIpClient = null;
                ModbusUtil.SendDataEvent -= ModbusUtil_SendDataEvent;
                ModbusUtil.ReceiveDataEvent -= ModbusUtil_ReceiveDataEvent;
            }
        }

        private async Task ConnectToTcpIPAsync(string ipAddress, int port)
        {
            IsConnectEnable = false;
            string exMsg = string.Empty;
            _tcpIpClient = null;
            try
            {
                _tcpIpClient = new TcpClient();
                // 异步连接到指定的服务器IP和端口
                await _tcpIpClient.ConnectAsync(ipAddress, port);
                _receiveToTcpIpCTS = new CancellationTokenSource();
                // 启动一个异步任务接收来自服务器的消息
                ReceiveToTcp();
            }
            catch (Exception ex)
            {
                exMsg = ex.Message;
                _tcpIpClient?.Dispose();
                _tcpIpClient = null;
                IsConnect = false;
            }
            MsgList.Add(_tcpIpClient == null ? exMsg : "创建连接成功！");
            IsConnectEnable = true;

        }

        private async Task ConnectToModbusTcpAsync(string ipAddress, int port)
        {
            IsConnectEnable = false;
            string exMsg = string.Empty;
            _master = await Task.Run(() =>
            {
                _modbusTcpClient = null;
                IModbusMaster master = null;
                try
                {
                    _modbusTcpClient = new TcpClient(ipAddress, port);
                    master = _factory.CreateMaster(_modbusTcpClient);
                    ModbusUtil.SendDataEvent += ModbusUtil_SendDataEvent;
                    ModbusUtil.ReceiveDataEvent += ModbusUtil_ReceiveDataEvent;
                }
                catch (Exception ex)
                {
                    exMsg = ex.Message;
                    _modbusTcpClient?.Dispose();
                    _modbusTcpClient = null;
                    master?.Dispose();
                    master = null;
                    IsConnect = false;
                }
                return master;
            });
            MsgList.Add(_master == null ? exMsg : "创建连接成功！");
            IsConnectEnable = true;
        }

        private void ModbusUtil_SendDataEvent(byte[] data)
        {
            string dataStr = string.Join(" ", data.Select(b => b.ToString("X2")));
            MsgList.Add(string.Format("发送 - [{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dataStr));
        }

        private void ModbusUtil_ReceiveDataEvent(byte[] data)
        {
            string dataStr = string.Join(" ", data.Select(b => b.ToString("X2")));
            MsgList.Add(string.Format("接收 - [{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dataStr));
        }
    }
}
