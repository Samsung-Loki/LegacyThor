using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hreidmar.Library.Exceptions;
using Hreidmar.Library.Packets;
using Hreidmar.Library.Packets.Inbound;
using Hreidmar.Library.Packets.Outbound;
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
        /// <summary>
        /// Settings for the DeviceSession
        /// </summary>
        public class OptionsClass
        {
            /// <summary>
            /// Automatically reboot after session ends.
            /// </summary>
            public bool AutoReboot = false;
            
            /// <summary>
            /// Automatically perform handshake after initialization.
            /// </summary>
            public bool AutoHandshake = true;

            /// <summary>
            /// Makes DeviceSession think, that handshake was already done.
            /// Use this only if you closed Hreidmar and your decice was not replugged in.
            /// </summary>
            public bool ResumeUsbConnection = false;

            /// <summary>
            /// Makes DeviceSession think, that session already began.
            /// Use this only if you lost your USB connection to your device when a session was active. 
            /// </summary>
            public bool ResumeSession = false;
            
            /// <summary>
            /// Use this only if you want to enable T-Flash.
            /// You can't disable it until you reboot your device.
            /// </summary>
            public bool EnableTFlash = false;

            /// <summary>
            /// Protocol version. V4 is recommended.
            /// You can't change it until you end current session.
            /// </summary>
            public ProtocolVersion Protocol = ProtocolVersion.Version4;
        }
        
        // Samsung device detection
        public static readonly int SamsungKVid = 0x04E8;
        public static readonly int[] SamsungPids = { 0x6601, 0x685D, 0x68C3 };
        // LibUsb stuff
        private readonly MonoUsbSessionHandle _sessionHandle = new();
        private readonly MonoUsbDeviceHandle _deviceHandle;
        // USB connection stuff
        private int _alternateId = 0xFF;
        private int _interfaceId = 0xFF;
        private byte _readEndpoint = 0xFF;
        private byte _writeEndpoint = 0xFF;
        private UsbEndpointWriter _writer;
        private UsbEndpointReader _reader;
        private UsbDevice _device;
        private int _error;
        // File flashing stuff
        private int _packetsPerSequence = 800;
        private int _transferPacketSize = 131072;
        private int _transferTimeout = 30000;
        // Session
        public bool SessionBegan = false;
        public bool HanshakeDone = false;
        public bool TFlashEnabled = false;
        // Options
        private OptionsClass _options;
        public Action<string> LogFunction;

        /// <summary>
        /// Find a samsung device and initialize it
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="log">Logging</param>
        /// <exception cref="DeviceNotFoundException">No device was found</exception>
        public DeviceSession(OptionsClass options, Action<string> log)
        {
            LogFunction = log;
            _options = options;

            UsbRegistry found = null;
            foreach (UsbRegistry device in UsbDevice.AllDevices) {
                if (device.Vid != SamsungKVid || !SamsungPids.Contains(device.Pid)) continue;
                LogFunction($"Found device: {device.Vid}/{device.Pid}");
                found = device;
            }
            
            if (found == null) throw new DeviceNotFoundException("No Samsung devices were found!");
            LogFunction($"Selected device: {found.Vid}/{found.Pid}");
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
        /// <param name="log">Logging</param>
        public DeviceSession(UsbDevice device, OptionsClass options, Action<string> log)
        {
            LogFunction = log;
            _options = options;
            
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
        /// Apply new options
        /// </summary>
        public void ApplyChanges(OptionsClass options)
        {
            if (TFlashEnabled && options.EnableTFlash)
                throw new Exception("You can't disable TFlash until a reboot!");
            if (SessionBegan && options.ResumeSession)
                throw new Exception("Session already began, you can't resume it!");
            if (HanshakeDone && options.ResumeUsbConnection)
                throw new Exception("Handshake was already done, you can't resume it!");
            if (_options.AutoHandshake && !options.AutoHandshake)
                throw new Exception("Auto-handshake can't be disabled after it was already done!");
            if (!_options.AutoHandshake && options.AutoHandshake)
                throw new Exception("Auto-handshake can't be enabled after initialization was done!");
            if (options.Protocol != _options.Protocol && !SessionBegan)
                throw new Exception("Protocol can't be changed while you have an active session!");
            _options = options;
        }

        /// <summary>
        /// Initialize connection and required stuff
        /// </summary>
        private void Initialize()
        {
            void CheckForErrors() {
                if (_error == 0) return;
                var error = _error;
                Dispose();
                throw new Exception($"{error}");
            }
            
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
            if (_deviceHandle != null && _sessionHandle.IsInvalid && _deviceHandle.IsInvalid) {
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
            if (_options.AutoHandshake && !_options.ResumeUsbConnection) Handshake();
            if (_options.EnableTFlash) EnableTFlash();
            SessionBegan = _options.ResumeSession;
            HanshakeDone = _options.ResumeUsbConnection;
        }

        /// <summary>
        /// Perform a handshake (protocol initialization)
        /// </summary>
        public void Handshake()
        {
             LogFunction($"Doing handshake...");
             SendPacket(new HandshakePacket(), 6000);
             var packet = (IInboundPacket) new HandshakeResponse();
             ReadPacket(ref packet, 6000);
        }

        /// <summary>
        /// Write to underlying device
        /// </summary>
        /// <param name="data">Data buffer</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="wrote">Wrote total</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        public void Write(byte[] data, int timeout, out int wrote, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            if (sendEmptyBefore) {
                var code = _writer.Write(Array.Empty<byte>(), 100, out _);
                if (code != ErrorCode.Ok)
                    LogFunction($"Unable to send an empty packet before: {code}");
            }
            var code1 = _writer.Write(data, timeout, out wrote);
            if (code1 != ErrorCode.Ok)
                throw new Exception($"Unable to write: {code1}");
            if (sendEmptyAfter) {
                var code = _writer.Write(Array.Empty<byte>(), 100, out _);
                if (code != ErrorCode.Ok)
                    LogFunction($"Unable to send an empty packet after: {code}");
            }
        }

        /// <summary>
        /// Read from underlying device
        /// </summary>
        /// <param name="data">Buffer</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="read">Read total</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        public void Read(ref byte[] data, int timeout, out int read, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            if (sendEmptyBefore) {
                var code = _reader.Read(Array.Empty<byte>(), 100, out _);
                if (code != ErrorCode.Ok)
                    LogFunction($"Unable to read an empty packet before: {code}");
            }
            var code1 = _reader.Read(data, timeout, out read);
            if (code1 != ErrorCode.Ok)
                throw new Exception($"Unable to read: {code1}");
            if (sendEmptyAfter) {
                var code = _reader.Read(Array.Empty<byte>(), 100, out _);
                if (code != ErrorCode.Ok)
                    LogFunction($"Unable to read an empty packet after: {code}");
            }
        }

        /// <summary>
        /// Send a packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="sendEmptyBefore">Send an empty packet before transfer</param>
        /// <param name="sendEmptyAfter">Send an empty packet after transfer</param>
        public void SendPacket(IOutboundPacket packet, int timeout, bool sendEmptyBefore = false, bool sendEmptyAfter = false)
        {
            Write(packet.Pack(), timeout, out var wrote, sendEmptyBefore, sendEmptyAfter);
            if (wrote == 0)
                throw new Exception($"Sent nothing!");
        }

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
            if (read == 0)
                throw new Exception($"Received nothing!");
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
            LogFunction("Beginning session...");
            SendPacket(new SessionSetupPacket { Version = _options.Protocol }, 6000);
            var packet = (IInboundPacket) new SessionSetupResponse();
            ReadPacket(ref packet, 6000);
            var actualPacket = (SessionSetupResponse)packet;
            if (actualPacket.Flags != 0) {
                LogFunction("Changing packet size is not supported!");
                _transferTimeout = 120000;     // Two minutes...
                _transferPacketSize = 1048576; // 1 MiB
                _packetsPerSequence = 30;      // 30 MB per sequence
                SendPacket(new FilePartSizePacket { FileSize = _transferPacketSize }, 1000);
                ReadPacket(ref packet, 6000);
                actualPacket = (SessionSetupResponse)packet;
                if (actualPacket.Flags != 0)
                    throw new Exception($"Received {actualPacket.Flags} instead of 0.");
                LogFunction("Successfully changed packet size!");
            }
            
            LogFunction("Session began!");
            SessionBegan = true;
        }

        /// <summary>
        /// Is the device connected?
        /// </summary>
        /// <returns>Common sense</returns>
        public bool IsConnected()
            => _device.IsOpen;

        /// <summary>
        /// Enable T-Flash
        /// </summary>
        public void EnableTFlash()
        {
            if (TFlashEnabled)
                throw new Exception("TFlash cannot be disabled!");
            if (!SessionBegan) BeginSession();
            LogFunction("Enabling T-Flash...");
            SendPacket(new EnableTFlashPacket(), 6000);
            var packet = (IInboundPacket) new SessionSetupResponse();
            ReadPacket(ref packet, 6000);
            var entire = (SessionSetupResponse) packet;
            if (entire.Flags != 0)
                throw new Exception($"Invalid response: {entire.Flags}");
            LogFunction("T-Flash enabled!");
            EndSession();
        }

        /// <summary>
        /// Dump device's PIT
        /// </summary>
        /// <param name="progress">Report progress</param>
        /// <returns>PIT data buffer</returns>
        public byte[] DumpPit(Action<int> progress)
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
            for (var i = 0; i < blocks; i++) {
                var last = i + 1 == blocks;
                SendPacket(new DumpPitPacket { Block = i }, 6000);
                Array.Resize(ref tmpbuf, 500);
                Read(ref tmpbuf, 6000, out var read);
                Array.Resize(ref tmpbuf, Math.Min(read, size - i * 500));
                if (read != 500 && !last)
                    throw new Exception($"Read not enough bytes: {read}");
                buf.AddRange(tmpbuf);
                progress(read);
            }
            SendPacket(new EndPitPacket(), 6000);
            packet = new PitResponse();
            ReadPacket(ref packet, 6000);
            return buf.ToArray();
        } 
        
        /// <summary>
        /// Report total byte size
        /// </summary>
        /// <param name="length">Total byte size</param>
        public void ReportTotalBytes(IEnumerable<ulong> length)
        {
            if (!SessionBegan) BeginSession();
            // Doing this fixes invalid percentage drawing.
            // For no reason it adds entire packet's size,
            // even if it's bigger that size reported. 
            var total = length.Aggregate<ulong, ulong>(0, (current, i)
                => current + (ulong)Math.Ceiling((double)i / _transferPacketSize) * (ulong)_transferPacketSize);
            SendPacket(new TotalBytesPacket { Length = total }, 6000);
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
            LogFunction("Rebooting...");
            SendPacket(new RebootDevicePacket(), 6000);
            var packet = (IInboundPacket) new EndSessionResponse();
            ReadPacket(ref packet, 6000);
            LogFunction("Device rebooted!");
        }

        /// <summary>
        /// Ends current session
        /// </summary>
        public void EndSession()
        {
            if (!SessionBegan) 
                throw new Exception("Session has not started yet!");
            LogFunction("Ending session...");
            SendPacket(new EndSessionPacket(), 6000);
            var packet = (IInboundPacket) new EndSessionResponse();
            ReadPacket(ref packet, 6000);

            if (_options.AutoReboot) Reboot();
            LogFunction("Session ended!");
            SessionBegan = false;
        }

        /// <summary>
        /// Flash partition info table
        /// </summary>
        /// <param name="data">PIT buffer</param>
        public void FlashPit(byte[] data)
        {
            LogFunction("Flashing PIT...");
            SendPacket(new BeginPitFlashPacket(), 6000);
            var packet = (IInboundPacket) new PitResponse();
            ReadPacket(ref packet, 6000);
            SendPacket(new PitFlashPacket { Length = data.Length }, 6000);
            ReadPacket(ref packet, 6000);
            Write(data, 6000, out var wrote);
            if (wrote != data.Length)
                throw new Exception($"PIT flash failed: Buffer length {data.Length}, sent only {wrote}");
            ReadPacket(ref packet, 6000);
            SendPacket(new EndPitPacket(), 6000);
            ReadPacket(ref packet, 6000);
            LogFunction("Successful transfer!");
        }

        /// <summary>
        /// Flash a file
        /// </summary>
        /// <param name="progress">Report progress</param>
        /// <param name="stream">Stream</param>
        /// <param name="entry">PIT entry</param>
        public void FlashFile(Stream stream, PitEntry entry, Action<int> progress)
        {
            LogFunction($"Flashing {entry.PartitionName}...");
            stream.Seek(0, SeekOrigin.Begin); // Failsafe
            SendPacket(new BeginFileFlashPacket(), 6000);
            var packet = (IInboundPacket) new FileResponse();
            ReadPacket(ref packet, 6000);
            
            var sequence = _packetsPerSequence * _transferPacketSize;
            // ReSharper disable once PossibleLossOfFraction
            var count = (int)Math.Ceiling((double)stream.Length / sequence);
            for (var i = 0; i < count; i++) {
                long read = i * sequence;
                long left = stream.Length - read;
                var fileParts = (int)Math.Min(_packetsPerSequence, Math.Ceiling((double)left / _transferPacketSize));
                var size = (int)Math.Min(sequence, left);
                SendPacket(new BeginFileSequenceFlashPacket { Length = size }, 6000);
                packet = new FileResponse();
                ReadPacket(ref packet, 6000);
                for (int j = 0; j < fileParts; j++) {
                    var read2 = j * _transferPacketSize;
                    var left2 = size - read2;
                    var size2 = Math.Min(_transferPacketSize, left2);
                    var buf = new byte[_transferPacketSize];
                    var bytes = stream.Read(buf, 0, size2);
                    while (bytes != size2) // Workaround if data is not available yet
                        bytes += stream.Read(buf, 0, size2 - bytes);
                    Write(buf, 6000, out var wrote);
                    if (wrote != buf.Length)
                        throw new Exception($"Buffer size {buf.Length}, sent {wrote}");
                    packet = new FilePartResponse();
                    ReadPacket(ref packet, 6000);
                    var actual = (FilePartResponse) packet;
                    if (actual.Index != j)
                        throw new Exception($"Actual index {j}, received index {actual.Index}");
                    progress(wrote);
                }

                switch (entry.BinaryType) {
                    case PitEntry.BinaryTypeEnum.AP:
                        SendPacket(new EndFileSequencePhoneFlashPacket {
                            DeviceType = entry.DeviceType, 
                            Identifier = entry.Identifier,
                            IsLastSequence = i + 1 == count,
                            Length = size
                        }, 6000);
                        break;
                    case PitEntry.BinaryTypeEnum.CP:
                        SendPacket(new EndFileSequenceModemFlashPacket {
                            DeviceType = entry.DeviceType,
                            IsLastSequence = i + 1 == count,
                            Length = size
                        }, 6000);
                        break;
                    default:
                        throw new Exception($"Invalid BinaryType: {(int)entry.BinaryType}");
                }
                
                packet = new FileResponse();
                ReadPacket(ref packet, 6000);
            }
            LogFunction($"Done!");
        }

        /// <summary>
        /// Dispose current DeviceSession
        /// </summary>
        public void Dispose()
        {
            if (SessionBegan) EndSession();
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
