using System;
using System.Collections.Generic;
using Hreidmar.Enigma.Exceptions;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.LudnMonoLibUsb;
using LibUsbDotNet.Main;
using MonoLibUsb;
using Spectre.Console;

namespace Hreidmar.Enigma
{
    /// <summary>
    /// Samsung ODIN device session
    /// </summary>
    public class DeviceSession : IDisposable
    {
        // Samsung device detection
        public static readonly int SamsungKVid = 0x04E8;
        public static readonly int[] SamsungPids = { 0x6601, 0x685D, 0x68C3 };
        // LibUsb stuff
        private readonly MonoUsbSessionHandle _sessionHandle = new();
        private MonoUsbDeviceHandle _deviceHandle;
        // USB connection stuff
        private int _alternateId = 0xFF;
        private int _interfaceId = 0xFF;
        private byte _readEndpoint = 0xFF;
        private byte _writeEndpoint = 0xFF;
        private UsbEndpointWriter _writer;
        private UsbEndpointReader _reader;
        private UsbRegistry _registry;
        private UsbDevice _device;
        private int _error;
        // Session
        public bool SessionBegan;
        public bool HandshakeDone;
        public bool TFlashEnabled = false;
        public Dictionary<string, string> Information;
        // Options
        public Action<string> LogFunction;

        /// <summary>
        /// Initialize an USB device
        /// </summary>
        /// <param name="device">USB device</param>
        /// <param name="options">Options</param>
        /// <param name="log">Logging</param>
        public DeviceSession(UsbRegistry device, Action<string> log)
        {
            LogFunction = log;

            LogFunction($"Last error: {UsbDevice.LastErrorNumber} {UsbDevice.LastErrorString}");
            _device = device.Device;
            _registry = device;
            Initialize();
        }

        /// <summary>
        /// Initialize connection and required stuff
        /// </summary>
        private void Initialize()
        {
            void CheckForErrors() {
                if (_error == 0) return;
                var error = _error;
                Dispose(); throw new Exception($"{error}");
            }

            if (!_device.Open())
                throw new Exception($"Unable to open device: {UsbDevice.LastErrorNumber} {UsbDevice.LastErrorString}");

            LogFunction($"Driver mode: {_device.DriverMode}");
            LogFunction($"Product: {_device.Info.ProductString}");
            bool found = false;
            LogFunction($"Interfaces total: {_device.Configs[0].InterfaceInfoList.Count}!");
            foreach (UsbInterfaceInfo interfaceInfo in _device.Configs[0].InterfaceInfoList) {
                byte possibleReadEndpoint = 0xFF;
                byte possibleWriteEndpoint = 0xFF;
                _interfaceId = interfaceInfo.Descriptor.InterfaceID;
                _alternateId = interfaceInfo.Descriptor.AlternateID;
                LogFunction($"Interface 0x{_interfaceId:X2}/0x{_alternateId:X2}: {interfaceInfo.EndpointInfoList.Count}/{interfaceInfo.Descriptor.Class}");
                if (interfaceInfo.EndpointInfoList.Count != 2) continue;
                if (interfaceInfo.Descriptor.Class != ClassCodeType.Data) continue;
                LogFunction($"Interface is valid!");
                foreach (var endpoint in interfaceInfo.EndpointInfoList) {
                    var id = endpoint.Descriptor.EndpointID;
                    if (id is >= 0x81 and <= 0x8F)
                        possibleReadEndpoint = id;
                    else if (id is >= 0x01 and <= 0x0F)
                        possibleWriteEndpoint = id;
                    else throw new DeviceConnectionFailedException($"Invalid EndpointID!");
                    LogFunction($"Endpoint 0x{id:X2}: {endpoint.Descriptor.MaxPacketSize}/{endpoint.Descriptor.Interval}/{endpoint.Descriptor.Refresh}");
                }

                if (possibleReadEndpoint == 0xFF || possibleWriteEndpoint == 0xFF) continue;
                found = true;
                LogFunction($"Endpoints are valid!");
                _readEndpoint = possibleReadEndpoint;
                _writeEndpoint = possibleWriteEndpoint;
            }
            
            if (!found)
                throw new DeviceConnectionFailedException("No valid interfaces found!");
            if (_device is MonoUsbDevice mono && !_sessionHandle.IsInvalid) {
                _deviceHandle = mono.Profile.OpenDeviceHandle();
                if (_deviceHandle.IsInvalid)
                    throw new Exception("Handle is invalid!");
                _error = MonoUsbApi.SetConfiguration(_deviceHandle, _device.Configs[0].Descriptor.ConfigID); CheckForErrors();
                _error = MonoUsbApi.ClaimInterface(_deviceHandle, _interfaceId); CheckForErrors();
                _error = MonoUsbApi.SetInterfaceAltSetting(_deviceHandle, _interfaceId, _alternateId); CheckForErrors();
                if (MonoUsbApi.KernelDriverActive(_deviceHandle, _interfaceId) == 1) {
                    _error = MonoUsbApi.DetachKernelDriver(_deviceHandle, _interfaceId); CheckForErrors();
                    LogFunction($"Detached kernel driver!");
                }
            
                _error = MonoUsbApi.ResetDevice(_deviceHandle); CheckForErrors();
            }

            _writer = _device.OpenEndpointWriter((WriteEndpointID) _writeEndpoint);
            _reader = _device.OpenEndpointReader((ReadEndpointID) _readEndpoint);
        }

        /// <summary>
        /// Dispose current DeviceSession
        /// </summary>
        public void Dispose()
        {
            if (_deviceHandle != null) {
                _error = MonoUsbApi.ReleaseInterface(_deviceHandle, _interfaceId);
                if (_error != 0) 
                    throw new Exception($"{_error}");
                _deviceHandle.Close();
            }
            _sessionHandle.Close();
            _device.Close();
        }
    }
}
