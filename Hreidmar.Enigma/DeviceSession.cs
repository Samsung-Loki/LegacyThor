using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hreidmar.Enigma.Exceptions;
using Hreidmar.Enigma.Packets;
using Hreidmar.Enigma.Packets.Inbound;
using Hreidmar.Enigma.Packets.Outbound;
using Hreidmar.Enigma.PIT;
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
        // File flashing stuff
        private int _packetsPerSequence = 800;
        private int _transferPacketSize = 131072;
        private int _transferTimeout = 30000;
        // Session
        public bool SessionBegan;
        public bool HandshakeDone;
        public bool TFlashEnabled = false;
        public Dictionary<string, string> Information;
        // Options
        private OptionsClass _options;
        public Action<string> LogFunction;

        /// <summary>
        /// Initialize an USB device
        /// </summary>
        /// <param name="device">USB device</param>
        /// <param name="options">Options</param>
        /// <param name="log">Logging</param>
        public DeviceSession(UsbRegistry device, OptionsClass options, Action<string> log)
        {
            LogFunction = log;
            _options = options;

            LogFunction($"Last error: {UsbDevice.LastErrorNumber} {UsbDevice.LastErrorString}");
            _device = device.Device;
            _registry = device;
            Initialize();
        }

        /// <summary>
        /// Apply new options
        /// </summary>
        public void ApplyChanges(OptionsClass options)
        {
            if (TFlashEnabled && options.EnableTFlash)
                throw new Exception("You can't disable TFlash until a reboot!");
            if (options.ResumeSession != _options.ResumeSession)
                throw new Exception("Resume Session setting can't be changed!");
            if (options.ResumeUsbConnection != _options.ResumeUsbConnection)
                throw new Exception("Resume USB connection setting can't be changed!");
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
            SessionBegan = _options.ResumeSession;
            HandshakeDone = _options.ResumeUsbConnection;
            if (!_options.ResumeUsbConnection) Handshake();
            if (_options.EnableTFlash) EnableTFlash();
        }

        /// <summary>
        /// Perform a handshake (protocol initialization)
        /// </summary>
        public void Handshake()
        {
            LogFunction($"Getting device info...");
            SendPacket(new DeviceInfoPacket(), 6000);
            var packet = (IInboundPacket) new DeviceInfoResponse();
            ReadPacket(ref packet, 6000); 
            var actual = (DeviceInfoResponse) packet;
            Information = actual.Information;
            LogFunction($"Doing handshake...");
            SendPacket(new HandshakePacket(), 6000);
            packet = (IInboundPacket) new HandshakeResponse();
            ReadPacket(ref packet, 6000);
            HandshakeDone = true;
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
            => _registry.IsAlive;

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
        public byte[] DumpPit(Action<int, int> progress)
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
                progress(read, size);
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
        /// Shuts off your device
        /// </summary>
        public void Shutdown()
        {
            LogFunction("Shutting down...");
            SendPacket(new ShutdownDevicePacket(), 6000);
            var packet = (IInboundPacket) new EndSessionResponse();
            ReadPacket(ref packet, 6000);
            LogFunction("Device shut down!");
        }
        
        /// <summary>
        /// Shuts off your device
        /// </summary>
        public void PrintSalesCode()
        {
            LogFunction("Sending print sales code packet...");
            SendPacket(new PrintSalesCodePacket(), 6000);
            var packet = (IInboundPacket) new SessionSetupResponse();
            ReadPacket(ref packet, 6000);
            LogFunction("Done!");
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
        public void FlashFile(Stream stream, PitEntry entry, Action<ulong> progress)
        {
            LogFunction($"Flashing {entry.PartitionName}...");
            SendPacket(new BeginFileFlashPacket(), 6000);
            var packet = (IInboundPacket) new FileResponse();
            ReadPacket(ref packet, 6000);
            
            var sequence = _packetsPerSequence * _transferPacketSize;
            var done = 0ul;
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
                    var buf = new List<byte>();
                    var tmpbuf = new byte[size2];
                    var bytes = stream.Read(tmpbuf, 0, size2);
                    buf.AddRange(tmpbuf.Take(bytes));
                    while (bytes != size2) { // Workaround if data is not available yet
                        tmpbuf = new byte[size2 - bytes];
                        var read3 = stream.Read(tmpbuf, 0, size2 - bytes);
                        buf.AddRange(tmpbuf.Take(read3));
                        bytes += read3;
                    }
                    var arr = buf.ToArray();
                    Array.Resize(ref arr, _transferPacketSize);
                    Write(arr, 6000, out var wrote);
                    if (wrote != arr.Length)
                        throw new Exception($"Buffer size {arr.Length}, sent {wrote}");
                    packet = new FilePartResponse();
                    ReadPacket(ref packet, 6000);
                    var actual = (FilePartResponse) packet;
                    if (actual.Index != j)
                        throw new Exception($"Actual index {j}, received index {actual.Index}");
                    done += (ulong)wrote;
                    progress(done);
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
