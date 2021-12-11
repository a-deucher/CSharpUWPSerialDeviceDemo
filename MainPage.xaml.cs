using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SerialDeviceExample
{
    /// <summary>
    /// Uma página vazia que pode ser usada isoladamente ou navegada dentro de um Quadro.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<SerialConnectionInfo> _serialConnectionInfo = new List<SerialConnectionInfo>();
        SerialDevice SerialDeviceComm;
        private const uint _baudRate = 9600;
        private const SerialParity _parity = SerialParity.None;
        private const SerialStopBitCount _stopBitCount = SerialStopBitCount.One;
        private const ushort _dataBit = 8;
        private const SerialHandshake _serialHandshake = SerialHandshake.None;
        private DataWriter dataWriter;
        private DataReader dataReader;
        public MainPage()
        {
            this.InitializeComponent();
        }


        private async Task RefreshListAsync()
        {
            _serialConnectionInfo.Clear();
            cbListDevices.Items.Clear();
            string _serialSelector = SerialDevice.GetDeviceSelector();
            var infos = await DeviceInformation.FindAllAsync(_serialSelector);
            foreach (var info in infos)
            {
                SerialConnectionInfo serialConnectionInfo = new SerialConnectionInfo();
                serialConnectionInfo.PortName = info.Name;
                serialConnectionInfo.PortID = info.Id;
                _serialConnectionInfo.Add(serialConnectionInfo);
                cbListDevices.Items.Add(info.Name);
            }
        }

        private void RefreshList(object sender, RoutedEventArgs e)
        {
            _= RefreshListAsync();
        }

        private void click_SendString(object sender, RoutedEventArgs e)
        {
            _= SendStringDataAsync(tbSendString.Text);
        }
        private async Task SendStringDataAsync(string data)
        {
            dataWriter.WriteString(data);
            await dataWriter.StoreAsync();
        }
        private void click_ConfigPort(object sender, RoutedEventArgs e)
        {
            _ = ConnectionAsync();
        }

        private async Task ConnectionAsync()
        {
            int _indexPort = 0;
            foreach (var item in _serialConnectionInfo)
            {
                if (cbListDevices.SelectedItem.Equals(_serialConnectionInfo[_indexPort].PortName))
                {
                    Task.Run(async () => { SerialDeviceComm = await SerialDevice.FromIdAsync(_serialConnectionInfo[_indexPort].PortID); }).Wait();
                    SerialDeviceComm.BaudRate = _baudRate;
                    SerialDeviceComm.Parity = _parity;
                    SerialDeviceComm.StopBits = _stopBitCount;
                    SerialDeviceComm.DataBits = _dataBit;
                    SerialDeviceComm.Handshake = _serialHandshake;
                    // This will make that regardless of receiving bytes or not it will read and continue.
                    SerialDeviceComm.ReadTimeout = TimeSpan.FromMilliseconds(uint.MaxValue); // Secret is here 
                    dataWriter = new DataWriter(SerialDeviceComm.OutputStream);
                    dataReader = new DataReader(SerialDeviceComm.InputStream)
                    {
                        InputStreamOptions = InputStreamOptions.Partial
                    };
                }
                _indexPort++;
            }
        }

        private void click_ReadSerial(object sender, RoutedEventArgs e)
        {
            tbReceivedSerial.Text = ReadSerialAsync(2000).Result;
        }

        private async Task<string> ReadSerialAsync(int timeOut)
        {
            string _tempData = "";
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            while (sw.ElapsedMilliseconds < timeOut)
            {
                uint receivedStringSize = 0;
                Task.Delay(50).Wait(); // Time to take some bytes
                dataReader.InputStreamOptions = InputStreamOptions.Partial;
                Task.Run(async () => { receivedStringSize = await dataReader.LoadAsync(200); }).Wait();
                _tempData += dataReader.ReadString(receivedStringSize);
            }
            return _tempData;
        }
    }
    public class SerialConnectionInfo
    {
        public string PortID { get; set; }
        public string PortName { get; set; }
    }
}
