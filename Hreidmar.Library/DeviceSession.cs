using System;
using System.Linq;
using System.Text;
using Hreidmar.Library.Exceptions;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using Spectre.Console;

namespace Hreidmar.Library
{
    /// <summary>
    /// Samsung ODIN device session
    /// </summary>
    public class DeviceSession : IDisposable
    {
        private static readonly int SamsungKVid = 0x04E8;
        private static readonly int[] SamsungPids = { 0x6601, 0x685D, 0x68C3 };
        private readonly UsbRegistry _device;
        private IUsbDevice _wholeDevice;
        private UsbEndpointReader _reader;
        private UsbEndpointWriter _writer;
        private int _alternateId = 0xFF;
        private int _interfaceId = 0xFF;
        
        /// <summary>
        /// Find a samsung device and initialize it
        /// </summary>
        /// <exception cref="DeviceNotFoundException">No device was found</exception>
        public DeviceSession()
        {
            UsbRegistry found = null;
            foreach (UsbRegistry device in UsbDevice.AllDevices) {
                if (device.Vid == SamsungKVid && SamsungPids.Contains(device.Pid)) {
                    AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Found device: {device.Vid}/{device.Pid}");
                    found = device;
                }
            }
            
            if (found == null) throw new DeviceNotFoundException("No Samsung devices were found!");
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Selected device: {found.Vid}/{found.Pid}");
            _device = found;
            Initialize();
        }

        /// <summary>
        /// Initialize an USB device
        /// </summary>
        /// <param name="device">USB device</param>
        public DeviceSession(UsbRegistry device)
        {
            _device = device;
            Initialize();
        }

        /// <summary>
        /// Initialize connection and required stuff
        /// </summary>
        private void Initialize()
        {
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Driver mode: {_device.Device.DriverMode}");
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Device full name: {_device.FullName}");
            _wholeDevice = (IUsbDevice) _device.Device;
            if (_wholeDevice == null)
                throw new DeviceConnectionFailedException("Please install libusb(win32) driver for this device!");
            ReadEndpointID readEndpoint = 0;
            WriteEndpointID writeEndpoint = 0;
            bool found = false;
            foreach (UsbInterfaceInfo interfaceInfo in _device.Device.Configs[0].InterfaceInfoList) {
                byte possibleReadEndpoint = 0xFF;
                byte possibleWriteEndpoint = 0xFF;
                if (interfaceInfo.EndpointInfoList.Count != 2) continue;
                if (interfaceInfo.Descriptor.Class != ClassCodeType.Data) continue;
                _interfaceId = interfaceInfo.Descriptor.InterfaceID;
                _alternateId = interfaceInfo.Descriptor.AlternateID;
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Found valid interface: 0x{_interfaceId:X2}/0x{_alternateId:X2}!");
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
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Found valid endpoints: 0x{possibleReadEndpoint:X2}/0x{possibleWriteEndpoint:X2}!");
                readEndpoint = (ReadEndpointID) possibleReadEndpoint;
                writeEndpoint = (WriteEndpointID) possibleWriteEndpoint;
            }
            
            if (!found)
                throw new DeviceConnectionFailedException("No valid interfaces found!");

            if (!_wholeDevice.SetConfiguration(_device.Device.Configs[0].Descriptor.ConfigID)
                || !_wholeDevice.ClaimInterface(_interfaceId)
                || !_wholeDevice.SetAltInterface(_alternateId)
                || !_wholeDevice.Open())
                throw new DeviceConnectionFailedException("Unable setup device");
            
            _reader = _wholeDevice.OpenEndpointReader(readEndpoint, 512, EndpointType.Bulk);
            _writer = _wholeDevice.OpenEndpointWriter(writeEndpoint, EndpointType.Bulk);
            
            // Handshake
            var buf = Encoding.ASCII.GetBytes("ODIN");
            Array.Resize(ref buf, 7);
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Doing handshake...");
            _writer.Write(buf, 1000, out var wrote);
            buf = new byte[7];
            _reader.Read(buf, 1000, out var read);
            if (Encoding.ASCII.GetString(buf).Contains("LOKE")) AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Successful handshake!");
            else throw new DeviceConnectionFailedException($"Invalid handshake string: {Encoding.ASCII.GetString(buf)}");
        }

        /// <summary>
        /// Dispose current DeviceSession
        /// </summary>
        public void Dispose()
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _wholeDevice.ReleaseInterface(_interfaceId);
            _wholeDevice.Close();
        }
    }
}