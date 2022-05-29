// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Text;

namespace TheAirBlow.Thor.Enigma.Senders;

/// <summary>
/// A basic command
/// </summary>
public class BasicCmdSender : ISender
{
    public byte[] Send()
        => _buffer;
    
    private byte[] _buffer;

    /// <summary>
    /// A basic command without argument
    /// </summary>
    /// <param name="type">Packet Type</param>
    /// <param name="command">Command</param>
    public BasicCmdSender(int type, int command)
    {
        using var memory = new MemoryStream();
        using var binary = new BinaryWriter(memory);
        binary.Write(type); binary.Write(command);
        
        _buffer = memory.ToArray();
    }
    
    /// <summary>
    /// A basic command with Int32 arguments
    /// </summary>
    /// <param name="type">Packet Type</param>
    /// <param name="command">Command</param>
    /// <param name="arguments">Int32 Arguments</param
    public BasicCmdSender(int type, int command, params int[] arguments)
    {
        using var memory = new MemoryStream();
        using var binary = new BinaryWriter(memory);
        binary.Write(type); binary.Write(command);
        foreach (var i in arguments)
            binary.Write(i);
        
        _buffer = memory.ToArray();
    }
    
    /// <summary>
    /// A basic command with String argument
    /// </summary>
    /// <param name="type">Packet Type</param>
    /// <param name="command">Command</param>
    /// <param name="argument">String Argument</param
    public BasicCmdSender(int type, int command, string argument)
    {
        using var memory = new MemoryStream();
        using var binary = new BinaryWriter(memory);
        binary.Write(type); binary.Write(command);
        binary.Write(Encoding.UTF8
            .GetBytes(argument));
        
        _buffer = memory.ToArray();
    }
}