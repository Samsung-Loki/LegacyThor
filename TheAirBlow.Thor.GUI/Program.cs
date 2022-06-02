// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Serilog;
using Serilog.Core;

namespace TheAirBlow.Thor.GUI;

public static class Program
{
    /// <summary>
    /// Global SeriLog logger
    /// </summary>
    public static Logger Logger = new LoggerConfiguration()
        .WriteTo.File("main.log")
        .WriteTo.LogWindow(outputTemplate: 
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Console()
        .CreateLogger();
        
    /// <summary>
    /// Run the Hreidmar renderer
    /// </summary>
    [STAThread]
    public static void Main()
    {
        using var game = new MonoGameController();
        game.Run();
    }
}