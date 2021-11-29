using System;
using System.Linq;
using System.Text;
using Hreidmar.Library.Exceptions;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.LudnMonoLibUsb;
using LibUsbDotNet.Main;
using MonoLibUsb;
using Spectre.Console;

namespace Hreidmar.Library
{
    /// <summary>
    /// Samsung ODIN device session
    /// </summary>
    public class DeviceSession : IDisposable
    {
        public static readonly int SamsungKVid = 0x04E8;
        public static readonly int[] SamsungPids = { 0x6601, 0x685D, 0x68C3 };
        private MonoUsbSessionHandle _sessionHandle = new MonoUsbSessionHandle();
        private MonoUsbDeviceHandle _deviceHandle = null;
        private MonoUsbError _error;
        private MonoUsbDevice _device;
        private int _alternateId = 0xFF;
        private int _interfaceId = 0xFF;
        private byte _readEndpoint = 0xFF;
        private byte _writeEndpoint = 0xFF;
        
        /// <summary>
        /// Find a samsung device and initialize it
        /// </summary>
        /// <exception cref="DeviceNotFoundException">No device was found</exception>
        public DeviceSession()
        {
            void CheckForErrors() {
                if (_error != MonoUsbError.Success) 
                    throw new Exception($"{_error}");
            }
            
            UsbRegistry found = null;
            foreach (UsbRegistry device in UsbDevice.AllLibUsbDevices) {
                if (device.Vid != SamsungKVid || !SamsungPids.Contains(device.Pid)) continue;
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Found device: {device.Vid}/{device.Pid}");
                found = device;
            }
            
            if (found == null) throw new DeviceNotFoundException("No Samsung devices were found!");
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Selected device: {found.Vid}/{found.Pid}");
            var mono = (MonoUsbDevice)found.Device;
            if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Access denied!");
            _deviceHandle = mono.Profile.OpenDeviceHandle();
            _device = mono;
            Initialize();
        }

        /// <summary>
        /// Initialize an USB device
        /// </summary>
        /// <param name="device">USB device</param>
        public DeviceSession(MonoUsbDevice device)
        {
            void CheckForErrors() {
                if (_error != MonoUsbError.Success) 
                    throw new Exception($"{_error}");
            }

            var mono = device;
            if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Access denied!");
            _deviceHandle = mono.Profile.OpenDeviceHandle();
            _device = device;
            Initialize();
        }

        /// <summary>
        /// Initialize connection and required stuff
        /// </summary>
        private void Initialize()
        {
            void CheckForErrors() {
                if (_error != MonoUsbError.Success) 
                    throw new Exception($"{_error}");
            }
            
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Driver mode: {_device.DriverMode}");
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Product: {_device.Info.ProductString}");
            if (_sessionHandle.IsInvalid || _deviceHandle.IsInvalid)
                throw new DeviceConnectionFailedException("Please install libusb(win32) driver for this device!");
            bool found = false;
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Interfaces total: {_device.Configs[0].InterfaceInfoList.Count}!");
            foreach (UsbInterfaceInfo interfaceInfo in _device.Configs[0].InterfaceInfoList) {
                byte possibleReadEndpoint = 0xFF;
                byte possibleWriteEndpoint = 0xFF;
                _interfaceId = interfaceInfo.Descriptor.InterfaceID;
                _alternateId = interfaceInfo.Descriptor.AlternateID;
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Interface 0x{_interfaceId:X2}/0x{_alternateId:X2}: {interfaceInfo.EndpointInfoList.Count}/{interfaceInfo.Descriptor.Class}");
                if (interfaceInfo.EndpointInfoList.Count != 2) continue;
                if (interfaceInfo.Descriptor.Class != ClassCodeType.Data) continue;
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Interface is valid!");
                foreach (var endpoint in interfaceInfo.EndpointInfoList) {
                    var id = endpoint.Descriptor.EndpointID;
                    if (id is >= 0x81 and <= 0x8F)
                        possibleReadEndpoint = id;
                    else if (id is >= 0x01 and <= 0x0F)
                        possibleWriteEndpoint = id;
                    else throw new DeviceConnectionFailedException($"Invalid EndpointID!");
                    
                    AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Endpoint 0x{id:X2}: {endpoint.Descriptor.MaxPacketSize}/{endpoint.Descriptor.Interval}/{endpoint.Descriptor.Refresh}");
                }

                if (possibleReadEndpoint == 0xFF || possibleWriteEndpoint == 0xFF) continue;
                found = true;
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Endpoints are valid!");
                _readEndpoint = possibleReadEndpoint;
                _writeEndpoint = possibleWriteEndpoint;
            }
            
            if (!found)
                throw new DeviceConnectionFailedException("No valid interfaces found!");

            _error = (MonoUsbError) MonoUsbApi.SetConfiguration(_deviceHandle, _device.Configs[0].Descriptor.ConfigID); CheckForErrors();
            _error = (MonoUsbError) MonoUsbApi.ClaimInterface(_deviceHandle, _interfaceId); CheckForErrors();
            _error = (MonoUsbError) MonoUsbApi.SetInterfaceAltSetting(_deviceHandle, _interfaceId, _alternateId); CheckForErrors();
            if (MonoUsbApi.KernelDriverActive(_deviceHandle, _interfaceId) == 1) {
                _error = (MonoUsbError) MonoUsbApi.DetachKernelDriver(_deviceHandle, _interfaceId); CheckForErrors();
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Detached kernel driver!");
            }
            
            // Handshake
            _error = (MonoUsbError) MonoUsbApi.ResetDevice(_deviceHandle); CheckForErrors();
            var buf = Encoding.ASCII.GetBytes("ODIN");
            Array.Resize(ref buf, 7);
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Doing handshake...");
            _error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _writeEndpoint, buf, buf.Length, out var sent, 1000); CheckForErrors();
            buf = new byte[7];
            _error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _readEndpoint, buf, buf.Length, out var received, 1000); CheckForErrors();
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Sent {sent}, received {received}");
            if (Encoding.ASCII.GetString(buf).Contains("LOKE")) AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Successful handshake!");
            else throw new DeviceConnectionFailedException($"Invalid handshake string {Encoding.ASCII.GetString(buf)}");
        }

        /// <summary>
        /// Dispose current DeviceSession
        /// </summary>
        public void Dispose()
        {
            void CheckForErrors() {
                if (_error != MonoUsbError.Success) 
                    throw new Exception($"{_error}");
            }
            
            _error = (MonoUsbError) MonoUsbApi.ReleaseInterface(_deviceHandle, _interfaceId); CheckForErrors();
            _deviceHandle.Close();
        }
    }
}