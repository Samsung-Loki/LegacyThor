// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
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
    /// MonoLibUsb session handle
    /// </summary>
    private readonly MonoUsbSessionHandle _sessionHandle = new();
        
    /// <summary>
    /// MonoLibUsb device handle
    /// </summary>
    private MonoUsbDeviceHandle _deviceHandle;

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
    /// MonoLibUsb error code
    /// </summary>
    private int _error;
        
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
    public enum ProtocolTypeEnum { Loke, Odin }

    /// <summary>
    /// Protocol type (Loke/Odin)
    /// </summary>
    public ProtocolTypeEnum ProtocolType;

    /// <summary>
    /// Initialize an USB device
    /// </summary>
    /// <param name="device">USB device</param>
    /// <param name="logger">Logging</param>
    public DeviceSession(UsbRegistry device, Logger logger)
    {
        _logger = logger;
        _device = device.Device;
        Initialize();
        DetectProtocol();
    }

    /// <summary>
    /// Check for errors
    /// </summary>
    private void CheckForErrors()
    {
        if (_error == 0) return; var error = _error; Dispose(); 
        throw new DeviceConnectionFailedException(error.ToString());
    }
        
    // Only used in initialization
    private int _alternateId = 0xFF;
    private int _interfaceId = 0xFF;
    private byte _readEndpoint = 0xFF;
    private byte _writeEndpoint = 0xFF;

    /// <summary>
    /// Initialize connection and required stuff
    /// </summary>
    private void Initialize()
    {
        if (!_device.Open())
            throw new DeviceConnectionFailedException($"Unable to open device!");

        _logger.Information("Initializing DeviceSession...");
        _logger.Information($"Driver mode: {_device.DriverMode}");
        _logger.Information($"Product: {_device.Info.ProductString}");
        bool found = false;
        _logger.Information($"Interfaces total: {_device.Configs[0].InterfaceInfoList.Count}!");
        foreach (UsbInterfaceInfo interfaceInfo in _device.Configs[0].InterfaceInfoList) {
            byte possibleReadEndpoint = 0xFF;
            byte possibleWriteEndpoint = 0xFF;
            _interfaceId = interfaceInfo.Descriptor.InterfaceID;
            _alternateId = interfaceInfo.Descriptor.AlternateID;
            _logger.Information($"Interface 0x{_interfaceId:X2}/0x{_alternateId:X2}: " +
                                $"{interfaceInfo.EndpointInfoList.Count}/{interfaceInfo.Descriptor.Class}");
            if (interfaceInfo.EndpointInfoList.Count != 2) continue;
            if (interfaceInfo.Descriptor.Class != ClassCodeType.Data) continue;
            _logger.Information($"This interface is valid!");
            foreach (var endpoint in interfaceInfo.EndpointInfoList) {
                var id = endpoint.Descriptor.EndpointID;
                if (id is >= 0x81 and <= 0x8F)
                    possibleReadEndpoint = id;
                else if (id is >= 0x01 and <= 0x0F)
                    possibleWriteEndpoint = id;
                else throw new DeviceConnectionFailedException($"Invalid EndpointID!");
                _logger.Information($"Endpoint 0x{id:X2}: " +
                                    $"{endpoint.Descriptor.MaxPacketSize}/{endpoint.Descriptor.Interval}/{endpoint.Descriptor.Refresh}");
            }

            if (possibleReadEndpoint == 0xFF || possibleWriteEndpoint == 0xFF) continue;
            found = true;
            _logger.Information($"Interface's endpoints are valid!");
            _readEndpoint = possibleReadEndpoint;
            _writeEndpoint = possibleWriteEndpoint;
        }
            
        if (!found)
            throw new DeviceConnectionFailedException("Couldn't find any valid endpoints!");
        if (_device is MonoUsbDevice mono && !_sessionHandle.IsInvalid) {
            _deviceHandle = mono.Profile.OpenDeviceHandle();
            if (_deviceHandle.IsInvalid)
                throw new Exception("Handle is invalid!");
            _error = MonoUsbApi.SetConfiguration(_deviceHandle, _device.Configs[0].Descriptor.ConfigID); CheckForErrors();
            _error = MonoUsbApi.ClaimInterface(_deviceHandle, _interfaceId); CheckForErrors();
            _error = MonoUsbApi.SetInterfaceAltSetting(_deviceHandle, _interfaceId, _alternateId); CheckForErrors();
            if (MonoUsbApi.KernelDriverActive(_deviceHandle, _interfaceId) == 1) {
                _error = MonoUsbApi.DetachKernelDriver(_deviceHandle, _interfaceId); CheckForErrors();
                _logger.Information($"Kernel driver is active, detached!");
            }
            
            _error = MonoUsbApi.ResetDevice(_deviceHandle); CheckForErrors();
        }

        _writer = _device.OpenEndpointWriter((WriteEndpointID) _writeEndpoint);
        _reader = _device.OpenEndpointReader((ReadEndpointID) _readEndpoint);
        _logger.Information("Initialization done!");
    }

    /// <summary>
    /// Detect device's protocol
    /// </summary>
    private void DetectProtocol()
    {
        // Loke Protocol
        var protocol = (Protocol) new LokeProtocol(_writer, _reader);
        if (protocol.Handshake()) {
            ProtocolType = ProtocolTypeEnum.Loke;
            Protocol = protocol;
        }
        
        // Odin Protocol
        protocol = (Protocol) new OdinProtocol(_writer, _reader);
        if (protocol.Handshake()) {
            ProtocolType = ProtocolTypeEnum.Odin;
            Protocol = protocol;
        }

        throw new UnexpectedErrorException("Unknown bootloader!");
    }

    /// <summary>
    /// Dispose current DeviceSession
    /// </summary>
    public void Dispose()
    {
        if (_deviceHandle != null) {
            _error = MonoUsbApi.ReleaseInterface(_deviceHandle, _interfaceId);
            CheckForErrors(); _deviceHandle.Close();
        }
        _sessionHandle.Close();
        _device.Close();
    }
}