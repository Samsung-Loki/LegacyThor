// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;
using LibUsbDotNet;
using TheAirBlow.Thor.Enigma.Exceptions;
using TheAirBlow.Thor.Enigma.Protocols.Odin;
using TheAirBlow.Thor.Enigma.Receivers;
using TheAirBlow.Thor.Enigma.Senders;

namespace TheAirBlow.Thor.Enigma.Protocols;

/// <summary>
/// Odin v2/v3 protocol
/// </summary>
public class OdinProtocol : Protocol
{
    public int SequenceSize = 131072 * 800;
    public int PartsPerSequence = 800;
    public int FilePartSize = 131072;
    public int FlashTimeout = 30000;
    public bool CompressedSupported;
    
    private bool _done;
    
    /// <summary>
    /// Create a new instance of OdinProtocol
    /// </summary>
    /// <param name="writer">Writer</param>
    /// <param name="reader">Reader</param>
    public OdinProtocol(UsbEndpointWriter writer, 
        UsbEndpointReader reader) : base(writer, reader) { }

    /// <summary>
    /// Do a handshake and start a session
    /// </summary>
    public override bool Handshake()
    {
        var rec = Send(new StringSender("ODIN"),
            new StringAck("LOKE"));
        if (rec != null) {
            _done = true;
            var data = (BasicCmdReceiver)Send(new BasicCmdSender((int)PacketType.SessionStart, 
                    (int)SessionStart.BeginSession, 0x04),
                new BasicCmdReceiver((int)PacketType.SessionStart), 
                true);
            
            // Check for worst case scenario - Odin v1
            if (data.Arguments[0] != 0) {
                SequenceSize = 1048576 * 30;
                FilePartSize = 1048576;
                PartsPerSequence = 30;
                FlashTimeout = 120000;

                CompressedSupported = ((data.Arguments[0] >> 8) & 0xf0) == 0x80;
                Send(new BasicCmdSender((int)PacketType.SessionStart, 
                        (int)SessionStart.FilePartSize, FilePartSize),
                    new ByteAck((int)PacketType.SessionStart), 
                    true);
            }
        }
        return rec != null;
    }

    /// <summary>
    /// Closes the session
    /// </summary>
    public override void Dispose()
    {
        if (_done) 
            Send(new BasicCmdSender((int)PacketType.SessionEnd, 
                    (int)SessionEnd.EndSession),
            new ByteAck((int)PacketType.SessionEnd), true);
    }

    /// <summary>
    /// Get Device Information
    /// </summary>
    /// <returns>Device Information</returns>
    public DeviceInfo GetDeviceInfo()
    {
        // Using RawByteBuffer everywhere cause old bootloaders
        // spit out AcKnOwLeDgMeNt instead for some reason...
        Send(new BasicCmdSender((int)PacketType.DeviceInfo, 0x00), 
            new RawByteBuffer());
        using var memory = new MemoryStream();
        var res2 = (RawByteBuffer)Send(new BasicCmdSender
                ((int)PacketType.DeviceInfo, 0x01, 0),
            new RawByteBuffer(), true);
        memory.Write(res2.Data);
        
        Send(new BasicCmdSender((int)PacketType.DeviceInfo, 0x02),
            new RawByteBuffer());

        return new DeviceInfo(memory.ToArray());
    }

    /// <summary>
    /// Dump PIT on device
    /// </summary>
    /// <param name="target">Target stream</param>
    public void DumpPit(Stream target)
    {
        var res = (BasicCmdReceiver)Send(new BasicCmdSender(
                (int)PacketType.PitXmit, (int)XmitShared.RequestDump),
            new BasicCmdReceiver((int)PacketType.PitXmit), true);
        var size = res.Arguments[0];
        for (var i = 0; i < Math.Floor((decimal)size / 500); i++) {
            var res2 = (RawByteBuffer)Send(new BasicCmdSender
                    ((int)PacketType.PitXmit, (int)XmitShared.Begin),
                new RawByteBuffer(), true);
            target.Write(res2.Data);
        }

        Send(zlpRead: true);
        Send(new BasicCmdSender((int)PacketType.PitXmit, 
                (int)XmitShared.End),
            new BasicCmdReceiver((int)PacketType.PitXmit), 
            true);
    }

    /// <summary>
    /// Reboot the device
    /// </summary>
    public void Reboot()
        => Send(new BasicCmdSender((int)PacketType.SessionEnd,
                (int)SessionEnd.Reboot),
            new ByteAck((int) PacketType.SessionEnd), true);
    
    /// <summary>
    /// Power off the device
    /// </summary>
    public void Shutdown()
        => Send(new BasicCmdSender((int)PacketType.SessionEnd,
                (int)SessionEnd.Shutdown),
            new ByteAck((int) PacketType.SessionEnd), true);
    
    /// <summary>
    /// Reboot into Odin
    /// </summary>
    public void OdinReboot()
        => Send(new BasicCmdSender((int)PacketType.SessionEnd,
                (int)SessionEnd.OdinReboot),
            new ByteAck((int) PacketType.SessionEnd), true);
    
    /// <summary>
    /// Erase UserData
    /// </summary>
    public void NandEraseAll()
        => Send(new BasicCmdSender((int)PacketType.SessionStart,
                (int)SessionStart.NandEraseAll),
            new ByteAck((int) PacketType.SessionStart), 
            timeout: 12000);
    
    /// <summary>
    /// Enable T-Flash
    /// </summary>
    public void EnableTFlash()
        => Send(new BasicCmdSender((int)PacketType.SessionStart, 
                (int)SessionStart.EnableTFlash),
            new ByteAck((int)PacketType.SessionStart),
            true);
    
    /// <summary>
    /// Send total transfer bytes
    /// </summary>
    /// <param name="total">Total</param>
    public void SendTotalBytes(long total)
        => Send(new BasicCmdSender((int)PacketType.SessionStart, 
                (int)SessionStart.TotalBytes, total),
            new ByteAck((int)PacketType.SessionStart),
            true);

    public void TestCmd()
        => Send(new BasicCmdSender(110, 0),
            new BasicCmdReceiver(110),
            true);
}