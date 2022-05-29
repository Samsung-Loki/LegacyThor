// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using TheAirBlow.Thor.Enigma.Exceptions;
using TheAirBlow.Thor.Enigma.Receivers;
using TheAirBlow.Thor.Enigma.Senders;

namespace TheAirBlow.Thor.Enigma.Protocols;

public abstract class Protocol : IDisposable
{
    /// <summary>
    /// USB writer
    /// </summary>
    private UsbEndpointWriter _writer;
        
    /// <summary>
    /// USB reader
    /// </summary>
    private UsbEndpointReader _reader;

    /// <summary>
    /// Create a new instance of Protocol
    /// </summary>
    /// <param name="writer">Writer</param>
    /// <param name="reader">Reader</param>
    protected Protocol(UsbEndpointWriter writer, UsbEndpointReader reader)
    {
        _writer = writer;
        _reader = reader;
    }
    
    /// <summary>
    /// Send something
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="receiver">Receiver (optional)</param>
    /// <param name="alwaysExpectData">Throw an exception if no data received</param>
    /// <returns>Receiver with received data, null if no data received</returns>
    /// <exception cref="UnexpectedErrorException">Unexpected Error</exception>
    protected IReceiver Send(ISender sender, IReceiver receiver = null, bool alwaysExpectData = false)
    {
        var buf = sender.Send();
        var code = _writer.Write(buf, 0, out var transferred);
        if (code != ErrorCode.Ok && code != ErrorCode.Success)
            throw new UnexpectedErrorException(
                $"LibUSB error: {code} ({UsbDevice.LastErrorString} {UsbDevice.LastErrorNumber})");
        if (buf.Length != transferred)
            throw new UnexpectedErrorException($"Sent {buf.Length}, transmitted {transferred}!");

        if (receiver == null) return null;
        buf = new byte[1024];
        code = _reader.Read(buf, 2000, out transferred);
        if (code != ErrorCode.Ok && code != ErrorCode.Success)
            throw new UnexpectedErrorException(
                $"LibUSB error: {code} ({UsbDevice.LastErrorString} {UsbDevice.LastErrorNumber})");
        if (transferred == 0) {
            if (alwaysExpectData)
                throw new UnexpectedErrorException("Expected at least some data, received zero!");
            return null;
        }
        receiver.Receive(buf);
        return receiver;
    }

    /// <summary>
    /// Do a handshake
    /// </summary>
    /// <returns>Success or not</returns>
    public virtual bool Handshake() { return false; }

    /// <summary>
    /// Dispose stuff
    /// </summary>
    public virtual void Dispose() { }
}