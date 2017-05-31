﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using nanoFramework.Tools.Debugger;
using nanoFramework.Tools.Debugger.Extensions;
using nanoFramework.Tools.Debugger.WireProtocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nanoFramework.Tools.VisualStudio.Extension.ToolWindow.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class DeviceExplorerViewModel : ViewModelBase, INotifyPropertyChanging
    {
        public const int WRITE_TO_OUTPUT_TOKEN = 1;
        public const int SELECTED_NULL_TOKEN = 2;

        // keep this here otherwise Fody won't be able to properly implement INotifyPropertyChanging
#pragma warning disable 67
        public event PropertyChangingEventHandler PropertyChanging;
#pragma warning restore 67


        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public DeviceExplorerViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                AvailableDevices = new ObservableCollection<NanoDeviceBase>();

                AvailableDevices.Add(new NanoDevice<NanoSerialDevice>() { Description = "Awesome nanodevice1" });
                AvailableDevices.Add(new NanoDevice<NanoSerialDevice>() { Description = "Awesome nanodevice2" });
            }
            else
            {
                // Code runs "for real"
                AvailableDevices = new ObservableCollection<NanoDeviceBase>();
            }

            SelectedDevice = null;
        }

        public INFSerialDebugClientService SerialDebugService { get; set; } = null;

        public void OnSerialDebugServiceChanged()
        {
            if (SerialDebugService != null)
            {
                SerialDebugService.SerialDebugClient.DeviceEnumerationCompleted += SerialDebugClient_DeviceEnumerationCompleted;
            }
        }

        private void SerialDebugClient_DeviceEnumerationCompleted(object sender, EventArgs e)
        {
            SerialDebugService.SerialDebugClient.DeviceEnumerationCompleted -= SerialDebugClient_DeviceEnumerationCompleted;
            SelectedTransportType = TransportType.Serial;
            UpdateAvailableDevices();
        }

        public ObservableCollection<NanoDeviceBase> AvailableDevices { get; set; }

        private void UpdateAvailableDevices()
        {
            switch (SelectedTransportType)
            {
                case TransportType.Serial:
                    //BusySrv.ShowBusy(Res.GetString("HC_Searching"));
                    AvailableDevices = new ObservableCollection<NanoDeviceBase>(SerialDebugService.SerialDebugClient.NanoFrameworkDevices);
                    SerialDebugService.SerialDebugClient.NanoFrameworkDevices.CollectionChanged += NanoFrameworkDevices_CollectionChanged;
                    //BusySrv.HideBusy();
                    break;

                case TransportType.Usb:
                    //BusySrv.ShowBusy(Res.GetString("HC_Searching"));
                    //AvailableDevices = new ObservableCollection<NanoDeviceBase>(UsbDebugService.UsbDebugClient.NanoFrameworkDevices);
                    //UsbDebugService.UsbDebugClient.NanoFrameworkDevices.CollectionChanged += NanoFrameworkDevices_CollectionChanged;
                    // if there's just one, select it
                    //SelectedDevice = (AvailableDevices.Count == 1) ? AvailableDevices.First() : null;
                    //BusySrv.HideBusy();
                    break;

                case TransportType.TcpIp:
                    // TODO
                    //BusySrv.ShowBusy("Not implemented yet! Why not give it a try??");
                    //await Task.Delay(2500);
                    //    AvailableDevices = new ObservableCollection<NanoDeviceBase>();
                    //    SelectedDevice = null;
                    //BusySrv.HideBusy();
                    break;
            }
        }

        private void NanoFrameworkDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // handle this according to the selected device type 
            switch (SelectedTransportType)
            {
                //case TransportType.Usb:
                //    AvailableDevices = new ObservableCollection<NanoDeviceBase>(UsbDebugService.UsbDebugClient.NanoFrameworkDevices);
                //    break;

                case TransportType.Serial:
                    AvailableDevices = new ObservableCollection<NanoDeviceBase>(SerialDebugService.SerialDebugClient.NanoFrameworkDevices);
                    break;

                default:
                    // shouldn't get here...
                    break;
            }

            // signal event that the devices collection has changed
            this.MessengerInstance.Send<NotificationMessage>(new NotificationMessage(""), MessagingTokens.NanoDevicesCollectionHasChanged);
        }

        public NanoDeviceBase SelectedDevice { get; set; }

        public void OnSelectedDeviceChanging()
        {
            Debug.WriteLine($"Selected device changing from {SelectedDevice?.Description}");

            // disconnect device becoming unselected
            SelectedDeviceDisconnect();
        }

        public void OnSelectedDeviceChanged()
        {
            Debug.WriteLine($"Selected device changed to {SelectedDevice.Description}");

            SelectedDeviceConnectionResult = PingConnectionResult.None;

            if (SelectedDevice != null)
            {
                SelectedDevice.DebugEngine.SpuriousCharactersReceived -= DebugEngine_SpuriousCharactersReceived;
                SelectedDevice.DebugEngine.SpuriousCharactersReceived += DebugEngine_SpuriousCharactersReceived;
            }

            // signal event that the selected device has changed
            this.MessengerInstance.Send<NotificationMessage>(new NotificationMessage(""), MessagingTokens.SelectedNanoDeviceHasChanged);
        }

        private void DebugEngine_SpuriousCharactersReceived(object sender, nanoFramework.Tools.Debugger.StringEventArgs e)
        {
            string textToSend = $"[{DateTime.Now.ToString()}] {e.EventText}";
            this.MessengerInstance.Send<NotificationMessage>(new NotificationMessage(textToSend), WRITE_TO_OUTPUT_TOKEN);
        }


        #region Transport

        public List<TransportType> AvailableTransportTypes { get; set; }

        public TransportType SelectedTransportType { get; set; }

        public void OnSelectedTransportTypeChanged()
        {
            UpdateAvailableDevices();
        }

        #endregion


        #region Ping

        public PingConnectionResult SelectedDeviceConnectionResult { get; set; }
        public bool ConnectionResultOk { get { return (SelectedDeviceConnectionResult == PingConnectionResult.Ok); } }
        public bool ConnectionResultError { get { return (SelectedDeviceConnectionResult == PingConnectionResult.Error); } }
        public bool Pinging { get { return (SelectedDeviceConnectionResult == PingConnectionResult.Busy); } }

        public async Task SelectedDevicePing()
        {
            //IsBusyHeader = true;

            SelectedDeviceConnectionResult = PingConnectionResult.Busy;
            try
            {
                PingConnectionType connection = await SelectedDevice.PingAsync();
                SelectedDeviceConnectionResult = (connection != PingConnectionType.NoConnection) ? PingConnectionResult.Ok : PingConnectionResult.Error;
            }
            catch
            {
                SelectedDeviceConnectionResult = PingConnectionResult.Error;
            }
            finally
            {
                //IsBusyHeader = false;
            }
        }

        #endregion


        #region Connect/Disconnect

        public ConnectionState ConnectionStateResult { get; set; } = ConnectionState.None;

        public bool Connected { get { return (ConnectionStateResult == ConnectionState.Connected); } }
        public bool Disconnected { get { return (ConnectionStateResult == ConnectionState.Disconnected); } }
        public bool Connecting { get { return (ConnectionStateResult == ConnectionState.Connecting); } }
        public bool Disconnecting { get { return (ConnectionStateResult == ConnectionState.Disconnecting); } }

        public void OnConnectionStateResultChanged()
        {
            // signal event that the connection state has changed
            this.MessengerInstance.Send<NotificationMessage>(new NotificationMessage(""), MessagingTokens.ConnectionStateResultHasChanged);
        }

        public async Task ConnectDisconnect()
        {
            if (ConnectionStateResult == ConnectionState.Connected)
            {
                SelectedDeviceDisconnect();
            }
            else
            {
                await SelectedDeviceConnect();
            }
        }

        private async Task SelectedDeviceConnect()
        {
            if (SelectedDevice != null)
            {
                Debug.WriteLine($"Trying to connect to {SelectedDevice.Description} ...");
                //IsBusyHeader = true;
                ConnectionStateResult = ConnectionState.Connecting;

                bool connectOk = await SelectedDevice.DebugEngine.ConnectAsync(3, 1000);

                ConnectionStateResult = connectOk ? ConnectionState.Connected : ConnectionState.Disconnected;
                //IsBusyHeader = false;

                //TODO
                //if (!connectOk)
                //{
                //    await DialogSrv.ShowMessageAsync(Res.GetString("HC_ConnectionError"));
                //}
            }
        }

        private void SelectedDeviceDisconnect()
        {
            if (SelectedDevice != null)
            {
                //IsBusyHeader = true;
                ConnectionStateResult = ConnectionState.Disconnecting;
 
                SelectedDevice.DebugEngine.Disconnect();

                ConnectionStateResult = ConnectionState.Disconnected;
                //IsBusyHeader = false;
            }
        }

        #endregion


        #region Device Capabilites

        public StringBuilder DeviceDeploymentMap { get; set; }

        public StringBuilder DeviceFlashSectorMap { get; set; }

        public StringBuilder DeviceMemoryMap { get; set; }

        public StringBuilder DeviceSystemInfo { get; set; }

        public int LastDeviceHash { get; set; }

        public async Task LoadDeviceInfoAsync()
        {
            // sanity check
            if (SelectedDevice == null)
            {
                return;
            }

            // if same device nothing to do here, exit
            if (SelectedDevice.Description.GetHashCode() == LastDeviceHash)
                return;

            // keep device description hash code to avoid get info twice
            LastDeviceHash = SelectedDevice.Description.GetHashCode();

            try
            {
                // get device info
                var di = await SelectedDevice.GetDeviceInfoAsync();
                var mm = await SelectedDevice.DebugEngine.GetMemoryMapAsync();
                var fm = await SelectedDevice.DebugEngine.GetFlashSectorMapAsync();
                var dm = await SelectedDevice.DebugEngine.GetDeploymentMapAsync();

                // load properties for maps
                DeviceMemoryMap = new StringBuilder(mm?.ToStringForOutput() ?? "Empty");
                DeviceFlashSectorMap = new StringBuilder(fm?.ToStringForOutput() ?? "Empty");
                DeviceDeploymentMap = new StringBuilder(dm?.ToStringForOutput() ?? "Empty");
                // and system
                DeviceSystemInfo = new StringBuilder(di?.ToString() ?? "Empty");
            }
            catch
            {
                // reset prop to force a new get on next time we navigate into this page
                LastDeviceHash = 0;
            }
        }

        #endregion


        #region messaging tokens

        public static class MessagingTokens
        {
            public static readonly Guid SelectedNanoDeviceHasChanged = new Guid("{C3173983-A19A-49DD-A4BD-F25D360F7334}");
            public static readonly Guid NanoDevicesCollectionHasChanged = new Guid("{3E8906F9-F68A-45B7-A0CE-6D42BDB22455}");
            public static readonly Guid ConnectionStateResultHasChanged = new Guid("{CBB58A61-51B0-4ABB-8484-5D44F84B6A3C}");
        }

        #endregion
    }
}