using ModbusAssistant.Enums;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusAssistant.Models
{
    public class ModbusDefinition : BindableBase
    {
        private byte _slaveID;

        public byte SlaveID
        {
            get { return _slaveID; }
            set
            {
                _slaveID = value;
                RaisePropertyChanged();
            }
        }

        private FunctionCodeType _functionCode;

        public FunctionCodeType FunctionCode
        {
            get { return _functionCode; }
            set
            {
                _functionCode = value;
                RaisePropertyChanged();
            }
        }

        private ushort _address;

        public ushort Address
        {
            get { return _address; }
            set
            {
                _address = value;
                RaisePropertyChanged();
            }
        }

        private ushort _quantity;

        public ushort Quantity
        {
            get { return _quantity; }
            set
            {
                _quantity = value;
                RaisePropertyChanged();
            }
        }

        private int _scanRate;

        public int ScanRate
        {
            get { return _scanRate; }
            set
            {
                _scanRate = value;
                RaisePropertyChanged();
            }
        }

    }
}
