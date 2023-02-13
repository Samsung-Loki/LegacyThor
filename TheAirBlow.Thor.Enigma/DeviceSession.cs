// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.LudnMonoLibUsb;
using LibUsbDotNet.Main;
using MonoLibUsb;
using Serilog.Core;
using TheAirBlow.Thor.Enigma.Exceptions;
using TheAirBlow.Thor.Enigma.Protocols;

namespace TheAirBlow.Thor.Enigma;

/// <summary>
/// Samsung ODIN device session
/// </summary>
public class DeviceSession : IDisposable
{
    /// <summary>
    /// Samsung vendor ID
    /// </summary>
    public static readonly int SamsungKVid = 0x04E8;
        
    /// <summary>
    /// Samsung product IDs
    /// </summary>
    public static readonly int[] SamsungPids = { 0x6601, 0x685D, 0x68C3 };

    /// <summary>
    /// USB writer
    /// </summary>
    private UsbEndpointWriter _writer;
        
    /// <summary>
    /// USB reader
    /// </summary>
    private UsbEndpointReader _reader;

    /// <summary>
    /// Selected USB Device
    /// </summary>
    private UsbDevice _device;

    /// <summary>
    /// Serilog Logger
    /// </summary>
    private Logger _logger;

    /// <summary>
    /// Detected protocol
    /// </summary>
    public Protocol Protocol;

    /// <summary>
    /// Loke/Odin protocol
    /// </summary>
    public enum ProtocolTypeEnum { Loke, Odin, Prompt }

    /// <summary>
    /// Protocol type (listed above)
    /// </summary>
    public ProtocolTypeEnum ProtocolType;
    
    public DeviceSession() {}

    /// <summary>
    /// Initialize an USB device
    /// </summary>
    /// <param name="device">USB device</param>
    /// <param name="logger">Logging</param>
    /// <param name="prompt">PROMPT console</param>
    public DeviceSession(UsbRegistry device, Logger logger, bool prompt = false)
    {
        _logger = logger;
        _device = device.Device;
        Initialize();
        if (prompt) {
            ProtocolType = ProtocolTypeEnum.Prompt;
            Protocol = new PromptProtocol(_writer, _reader);
        } else DetectProtocol();
    }

    // Only used in initialization
    private int _interfaceId = -1;
    private int _readEndpoint = -1;
    private int _writeEndpoint = -1;

    /// <summary>
    /// Initialize connection and required stuff
    /// </summary>
    private void Initialize()
    {
        if (!_device.Open())
            throw new DeviceConnectionFailedException($"Unable to open device!");

        _logger.Information($"=============================================");
        _logger.Information($"Driver mode: {_device.DriverMode}");
        _logger.Information($"Product: {_device.Info.ProductString}");
        _logger.Information($"Interfaces total: {_device.Configs[0].InterfaceInfoList.Count}!");
        _logger.Information($"=============================================");
        var found = false;
        if (_device is IUsbDevice whole) {
            if (!whole.SetConfiguration(_device.Configs[0].Descriptor.ConfigID))
                throw new DeviceConnectionFailedException("Failed to set configuration!");
            
            _logger.Information("Searching for RW data interface");
            foreach (var i in whole.Configs[0].InterfaceInfoList) {
                if (i.Descriptor.Class != ClassCodeType.Data) continue;
                if (i.EndpointInfoList.Count != 2) continue;
                _logger.Information("Found candidate, checking endpoints");
                foreach (var j in i.EndpointInfoList) {
                    if (j.Descriptor.EndpointID > 0x80)
                        _readEndpoint = j.Descriptor.EndpointID;
                    else _writeEndpoint = j.Descriptor.EndpointID;
                }

                if (_readEndpoint != -1 && _writeEndpoint != -1) {
                    _interfaceId = i.Descriptor.InterfaceID;
                    _logger.Information("Endpoints found successfully");
                    if (!whole.ClaimInterface(i.Descriptor.InterfaceID)) {
                        _logger.Error("Failed to claim interface!");
                        break;
                    }
                    if (!whole.SetAltInterface(i.Descriptor.AlternateID)) {
                        _logger.Error("Failed to set alternative interface!");
                        break;
                    }
                    
                    found = true; 
                    break;
                }

                _readEndpoint = -1;
                _writeEndpoint = -1;
            }
        }
            
        if (!found) throw new DeviceConnectionFailedException("Couldn't find any valid endpoints!");
        _reader = _device.OpenEndpointReader((ReadEndpointID)_readEndpoint);
        _writer = _device.OpenEndpointWriter((WriteEndpointID)_writeEndpoint);
        _logger.Information("Reader/writer ready");
    }

    /// <summary>
    /// Detect device's protocol
    /// </summary>
    private void DetectProtocol()
    {
        // Odin Protocol
        _logger.Information("Trying to connect with Odin protocol...");
        var protocol = (Protocol) new OdinProtocol(_writer, _reader);
        if (protocol.Handshake()) {
            ProtocolType = ProtocolTypeEnum.Odin;
            Protocol = protocol;
            return;
        }
        
        // Loke Protocol
        _logger.Information("Trying to connect with Loke protocol...");
        protocol = new LokeProtocol(_writer, _reader);
        if (protocol.Handshake()) {
            ProtocolType = ProtocolTypeEnum.Loke;
            Protocol = protocol;
            return;
        }

        throw new UnexpectedErrorException("Unknown bootloader!");
    }

    /// <summary>
    /// Dispose current DeviceSession
    /// </summary>
    public void Dispose()
    {
        if (_device is IUsbDevice dev)
            if (!dev.ReleaseInterface(_interfaceId))
                throw new UnexpectedErrorException("Unable to release interface!");
        _device.Close();
    }
}