using System;
using System.Collections.Generic;
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
        private int _alternateId = 0xFF;
        private int _interfaceId = 0xFF;
        private byte _readEndpoint = 0xFF;
        private byte _writeEndpoint = 0xFF;
        private int _packetsPerSequence = 800;
        private int _transferPacketSize = 131072;
        private int _transferTimeout = 30000;
        public bool SessionBegan = false; 
        private UsbEndpointWriter _writer;
        private UsbEndpointReader _reader;
        public OptionsClass Options;
        private UsbDevice _device;
        private int _error;

        /// <summary>
        /// Find a samsung device and initialize it
        /// </summary>
        /// <param name="options">Options</param>
        /// <exception cref="DeviceNotFoundException">No device was found</exception>
        public DeviceSession(OptionsClass options)
        {
            Options = options;

            UsbRegistry found = null;
            foreach (UsbRegistry device in UsbDevice.AllDevices) {
                if (device.Vid != SamsungKVid || !SamsungPids.Contains(device.Pid)) continue;
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Found device: {device.Vid}/{device.Pid}");
                found = device;
            }
            
            if (found == null) throw new DeviceNotFoundException("No Samsung devices were found!");
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Selected device: {found.Vid}/{found.Pid}");
            if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Access denied!");
            try {
                var mono = (MonoUsbDevice) found.Device;
                _deviceHandle = mono.Profile.OpenDeviceHandle();
            } catch { /* Ignore */ }
            _device = found.Device;
            Initialize();
        }

        /// <summary>
        /// Initialize an USB device
        /// </summary>
        /// <param name="device">USB device</param>
        /// <param name="options">Options</param>
        public DeviceSession(UsbDevice device, OptionsClass options)
        {
            Options = options;
            
            if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Access denied!");
            try {
                var mono = (MonoUsbDevice) device;
                _deviceHandle = mono.Profile.OpenDeviceHandle();
            } catch { /* Ignore */ }
            _device = device;
            Initialize();
        }

        /// <summary>
        /// Initialize connection and required stuff
        /// </summary>
        private void Initialize()
        {
            void CheckForErrors() {
                if (_error != 0) {
                    var error = _error;
                    Dispose();
                    throw new Exception($"[Initialize/CheckForErrors] {error}");
                }
            }
            
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Driver mode: {_device.DriverMode}");
            AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Product: {_device.Info.ProductString}");
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
            if (_deviceHandle != null && _sessionHandle.IsInvalid && _deviceHandle.IsInvalid) {
                _error = MonoUsbApi.SetConfiguration(_deviceHandle, _device.Configs[0].Descriptor.ConfigID); CheckForErrors();
                _error = MonoUsbApi.ClaimInterface(_deviceHandle, _interfaceId); CheckForErrors();
                _error = MonoUsbApi.SetInterfaceAltSetting(_deviceHandle, _interfaceId, _alternateId); CheckForErrors();
                if (MonoUsbApi.KernelDriverActive(_deviceHandle, _interfaceId) == 1) {
                    _error = MonoUsbApi.DetachKernelDriver(_deviceHandle, _interfaceId); CheckForErrors();
                    AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Detached kernel driver!");
                }
            
                _error = MonoUsbApi.ResetDevice(_deviceHandle); CheckForErrors();
            }

            _writer = _device.OpenEndpointWriter((WriteEndpointID) _writeEndpoint, EndpointType.Bulk);
            _reader = _device.OpenEndpointReader((ReadEndpointID) _readEndpoint, 512, EndpointType.Bulk);

            // Handshake
            if (!Options.Resume) {
                var buf = Encoding.ASCII.GetBytes("ODIN");
                AnsiConsole.MarkupLine($"[bold]<DeviceSession>[/] Doing handshake...");
                
                buf = new byte[4];
                
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
        public void Write(byte[] data, int timeout, out int wrote, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            if (sendEmptyBefore) _writer.Write(Array.Empty<byte>(), timeout, out _);
            _writer.Write(data, timeout, out wrote);
            if (sendEmptyAfter) _writer.Write(Array.Empty<byte>(), timeout, out _);
        }
        
        /// <summary>
        /// Read from underlying device
        /// </summary>
        /// <param name="data">Buffer</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        public void Read(ref byte[] data, int timeout, out int read, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            if (sendEmptyBefore) _reader.Read(Array.Empty<byte>(), timeout, out _);
            _reader.Read(data, timeout, out read);
            if (sendEmptyAfter) _reader.Read(Array.Empty<byte>(), timeout, out _);
        }

        /// <summary>
        /// Send a packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        public void SendPacket(IOutboundPacket packet, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
            => Write(packet.Pack(), timeout, out _, sendEmptyBefore, sendEmptyAfter);

        /// <summary>
        /// Read a packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        public void ReadPacket(ref IInboundPacket packet, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            var buf = new byte[packet.GetSize()];
            Read(ref buf, timeout, out var read, sendEmptyBefore, sendEmptyAfter);
            if (read != packet.GetSize())
                throw new Exception($"Expected {packet.GetSize()}, received {read}");
            packet.Unpack(buf);
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
            SendPacket(new SessionSetupPacket(), 1000);
            var packet = (IInboundPacket) new SessionSetupResponse();
            ReadPacket(ref packet, 6000);
            var actualPacket = (SessionSetupResponse)packet;
            if (actualPacket.Flags != 0) {
                AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Changing packet size is not supported!");
                _transferTimeout = 120000;     // Two minutes...
                _transferPacketSize = 1048576; // 1 MiB
                _packetsPerSequence = 30;      // 30 MB per sequence
                SendPacket(new FilePartSizePacket { FileSize = _transferPacketSize }, 1000);
                ReadPacket(ref packet, 6000);
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
            SendPacket(new DeviceTypePacket(), 6000);
            var packet = (IInboundPacket) new DeviceTypeResponse();
            ReadPacket(ref packet, 6000);
            var actual = (DeviceTypeResponse) packet;
            return actual.DeviceType;
        }
        
        /// <summary>
        /// Enable T-Flash
        /// </summary>
        public void EnableTFlash()
        {
            if (!SessionBegan) BeginSession();
            SendPacket(new EnableTFlashPacket(), 6000);
            var packet = (IInboundPacket) new SessionSetupResponse();
            ReadPacket(ref packet, 6000);
            var entire = (SessionSetupResponse) packet;
            if (entire.Flags != 0)
                throw new Exception($"Invalid response: {entire.Flags}");
        }

        /// <summary>
        /// Dump device's PIT
        /// </summary>
        /// <returns>PIT data buffer</returns>
        public byte[] DumpPit()
        {
            if (!SessionBegan) BeginSession();
            SendPacket(new BeginPitDumpPacket(), 6000);
            var packet = (IInboundPacket) new BeginPitDumpResponse();
            ReadPacket(ref packet, 6000);
            var entire = (BeginPitDumpResponse) packet;
            var size = entire.Length;
            var buf = new List<byte>();
            var blocks = (int)Math.Ceiling((decimal)size / 500);
            var tmpbuf = new byte[500];
            AnsiConsole.Progress().Start(ctx => {
                var task = ctx.AddTask("[yellow]Dumping PIT[/]", maxValue: size);
                for (var i = 0; i < blocks; i++) {
                    SendPacket(new DumpPitPacket { Block = i }, 6000);
                    var last = i + 1 == blocks;
                    Read(ref tmpbuf, 6000, out var read, sendEmptyAfter: last);
                    if (read != 500 && !last)
                        throw new Exception($"Read not enough bytes: {read}");
                    buf.AddRange(tmpbuf);
                    task.Increment(read);
                }
                task.StopTask();
                task.Description = "[green]Dumping PIT[/]";
            });
            SendPacket(new EndPitPacket(), 6000);
            packet = new PitResponse();
            ReadPacket(ref packet, 6000);
            return buf.ToArray();
        } 
        
        /// <summary>
        /// Report total byte size
        /// </summary>
        /// <param name="length">Total byte size</param>
        public void ReportTotalBytes(long length)
        {
            if (!SessionBegan) BeginSession();
            SendPacket(new TotalBytesPacket { Length = length }, 6000);
            var packet = (IInboundPacket) new SessionSetupResponse();
            ReadPacket(ref packet, 6000);
            var entire = (SessionSetupResponse) packet;
            if (entire.Flags != 0)
                throw new Exception($"Invalid response: {entire.Flags}");
        }

        /// <summary>
        /// Reboot your device
        /// </summary>
        public void Reboot()
        {
            if (!SessionBegan) BeginSession();
            SendPacket(new RebootDevicePacket(), 6000);
            var packet = (IInboundPacket) new EndSessionResponse();
            ReadPacket(ref packet, 6000);
        }

        /// <summary>
        /// Ends current session
        /// </summary>
        public void EndSession()
        {
            if (!SessionBegan)
                throw new Exception("Session has not started yet!");
            AnsiConsole.MarkupLine("[bold]<DeviceSession>[/] Ending session...");
            SendPacket(new EndSessionPacket(), 6000);
            var packet = (IInboundPacket) new EndSessionResponse();
            ReadPacket(ref packet, 6000);

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
                if (_error != 0) 
                    throw new Exception($"{_error}");
            }
            
            if (SessionBegan) EndSession();
            if (_deviceHandle != null) {
                _error = MonoUsbApi.ReleaseInterface(_deviceHandle, _interfaceId); CheckForErrors();
                _deviceHandle.Close();
            }
            _sessionHandle.Close();
            _device.Close();
        }
    }
}
