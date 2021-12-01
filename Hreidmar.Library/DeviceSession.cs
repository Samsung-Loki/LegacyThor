using System;
using System.Linq;
using System.Text;
using Hreidmar.Library.Exceptions;
using Hreidmar.Library.Packets;
using Hreidmar.Library.PIT;
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
        public class OptionsClass
        {
            public bool Reboot = false;
            public bool Resume = false;
        }
        
        public static readonly int SamsungKVid = 0x04E8;
        public static readonly int[] SamsungPids = { 0x6601, 0x685D, 0x68C3 };
        private readonly MonoUsbSessionHandle _sessionHandle = new();
        private readonly MonoUsbDeviceHandle _deviceHandle;
        private MonoUsbError _error;
        private MonoUsbDevice _device;
        private int _alternateId = 0xFF;
        private int _interfaceId = 0xFF;
        private byte _readEndpoint = 0xFF;
        private byte _writeEndpoint = 0xFF;
        private int _transferSequenceSize = 800;
        private int _transferPacketSize = 131072;
        private int _transferTimeout = 30000;
        public bool SessionBegan = false; 
        public OptionsClass Options;

        /// <summary>
        /// Find a samsung device and initialize it
        /// </summary>
        /// <param name="options">Options</param>
        /// <exception cref="DeviceNotFoundException">No device was found</exception>
        public DeviceSession(OptionsClass options)
        {
            Options = options;

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
        /// <param name="options">Options</param>
        public DeviceSession(MonoUsbDevice device, OptionsClass options)
        {
            Options = options;

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
                if (_error != MonoUsbError.Success) {
                    var error = _error;
                    Dispose();
                    throw new Exception($"[Initialize/CheckForErrors] {error}");
                }
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
            
            _error = (MonoUsbError) MonoUsbApi.ResetDevice(_deviceHandle); CheckForErrors();

            // Handshake
            if (!Options.Resume) {
                var buf = Encoding.ASCII.GetBytes("ODIN");
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Doing handshake...");
                _error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _writeEndpoint, buf, buf.Length, out _, 6000); CheckForErrors();
                buf = new byte[4];
                _error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _readEndpoint, buf, buf.Length, out _, 6000); CheckForErrors();
                if (Encoding.ASCII.GetString(buf).Contains("LOKE")) AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Successful handshake!");
                else throw new DeviceConnectionFailedException($"Invalid handshake string {Encoding.ASCII.GetString(buf)}");
            }
        }

        /// <summary>
        /// Write to underlying device
        /// </summary>
        /// <param name="data">Data buffer</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        /// <returns>Error code</returns>
        public int Write(byte[] data, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            if (sendEmptyBefore) {
                var error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _writeEndpoint, Array.Empty<byte>(),
                    0, out _, timeout);
                if (error != MonoUsbError.Success)
                    AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Failed to send an empty packet");
            }
            var code = MonoUsbApi.BulkTransfer(_deviceHandle, _writeEndpoint, data, data.Length, out _, timeout);
            if (sendEmptyAfter) {
                var error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _writeEndpoint, Array.Empty<byte>(),
                    0, out _, timeout);
                if (error != MonoUsbError.Success)
                    AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Failed to send an empty packet");
            }
            
            return code;
        }
        
        /// <summary>
        /// Read from underlying device
        /// </summary>
        /// <param name="data">Buffer</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        /// <returns>Error code</returns>
        public int Read(ref byte[] data, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            if (sendEmptyBefore) {
                var error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _readEndpoint, Array.Empty<byte>(),
                    0, out _, timeout);
                if (error != MonoUsbError.Success)
                    AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Failed to send an empty packet, ignoring...");
            }
            var code = MonoUsbApi.BulkTransfer(_deviceHandle, _readEndpoint, data, data.Length, out _, timeout);
            if (sendEmptyAfter) {
                var error = (MonoUsbError) MonoUsbApi.BulkTransfer(_deviceHandle, _readEndpoint, Array.Empty<byte>(),
                    0, out _, timeout);
                if (error != MonoUsbError.Success)
                    AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Failed to send an empty packet, ignoring...");
            }
            
            return code;
        }

        /// <summary>
        /// Send a packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        /// <returns>Error code</returns>
        public int SendPacket(IOutboundPacket packet, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
            => Write(packet.Pack(), timeout, sendEmptyBefore, sendEmptyAfter);

        /// <summary>
        /// Read a packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        /// <returns>Error code</returns>
        public int ReadPacket(ref IInboundPacket packet, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            var buf = new byte[packet.GetSize()];
            var code = Read(ref buf, timeout, sendEmptyBefore, sendEmptyAfter);
            packet.Unpack(buf);
            return code;
        }

        /// <summary>
        /// Begin a session
        /// </summary>
        /// <exception cref="Exception">Error occured</exception>
        public void BeginSession()
        {
            if (SessionBegan)
                throw new Exception("Session already began!");
            AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Beginning session...");
            var code = SendPacket(new SessionSetupPacket(), 1000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to send SessionSetupPacket: {(MonoUsbError)code}");
            var packet = (IInboundPacket) new SessionSetupResponse();
            code = ReadPacket(ref packet, 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to read SessionSetupResponse: {(MonoUsbError)code}");
            var actualPacket = (SessionSetupResponse)packet;
            if (actualPacket.Flags != 0) {
                AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Changing packet size is not supported!");
                _transferTimeout = 120000;     // Two minutes...
                _transferPacketSize = 1048576; // 1 MiB
                _transferSequenceSize = 30;    // 30 MB per sequence
                code = SendPacket(new FilePartSizePacket { FileSize = _transferPacketSize }, 1000);
                if ((MonoUsbError) code != MonoUsbError.Success)
                    throw new Exception($"Failed to send FilePartSizePacket: {(MonoUsbError)code}");
                code = ReadPacket(ref packet, 6000);
                if ((MonoUsbError) code != MonoUsbError.Success)
                    throw new Exception($"Failed to read SessionSetupPacket: {(MonoUsbError)code}");
                actualPacket = (SessionSetupResponse)packet;
                if (actualPacket.Flags != 0)
                    throw new Exception($"Received {actualPacket.Flags} instead of 0.");
                AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Successfully changed packet size!");
            }
            
            AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Session began!");
            SessionBegan = true;
        }

        /// <summary>
        /// Get device's type
        /// </summary>
        /// <returns>Device type</returns>
        public PitEntry.DeviceTypeEnum GetDeviceType()
        {
            if (!SessionBegan) BeginSession();
            var code = SendPacket(new DeviceTypePacket(), 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to send DeviceTypePacket: {(MonoUsbError)code}");
            var packet = (IInboundPacket) new DeviceTypeResponse();
            code = ReadPacket(ref packet, 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to read DeviceTypeResponse: {(MonoUsbError)code}");
            var actual = (DeviceTypeResponse) packet;
            return actual.DeviceType;
        }
        
        /// <summary>
        /// Enable T-Flash
        /// </summary>
        public void EnableTFlash()
        {
            if (!SessionBegan) BeginSession();
            var code = SendPacket(new EnableTFlashPacket(), 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to send EnableTFlashPacket: {(MonoUsbError)code}");
            var packet = (IInboundPacket) new SessionSetupResponse();
            code = ReadPacket(ref packet, 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to read SessionSetupResponse: {(MonoUsbError)code}");
        }

        /// <summary>
        /// Reboot your device
        /// </summary>
        public void Reboot()
        {
            if (!SessionBegan) BeginSession();
            var code = SendPacket(new RebootDevicePacket(), 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to send RebootDevicePacket: {(MonoUsbError)code}");
            var packet = (IInboundPacket) new EndSessionResponse();
            code = ReadPacket(ref packet, 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to read EndSessionResponse: {(MonoUsbError)code}");
        }

        /// <summary>
        /// Ends current session
        /// </summary>
        public void EndSession()
        {
            if (!SessionBegan)
                throw new Exception("Session has not started yet!");
            AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Ending session...");
            var code = SendPacket(new EndSessionPacket(), 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to send EndSessionPacket: {(MonoUsbError)code}");
            var packet = (IInboundPacket) new EndSessionResponse();
            code = ReadPacket(ref packet, 6000);
            if ((MonoUsbError) code != MonoUsbError.Success)
                throw new Exception($"Failed to read EndSessionResponse: {(MonoUsbError)code}");

            if (Options.Reboot) Reboot();
            AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Session ended!");
            SessionBegan = false;
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
            
            if (SessionBegan) EndSession();
            _error = (MonoUsbError) MonoUsbApi.ReleaseInterface(_deviceHandle, _interfaceId); CheckForErrors();
            _sessionHandle.Close();
            _deviceHandle.Close();
            _device.Close();
        }
    }
}
