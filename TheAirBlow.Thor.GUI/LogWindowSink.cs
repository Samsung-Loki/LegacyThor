// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using TheAirBlow.Thor.GUI.Windows;

namespace TheAirBlow.Thor.GUI;

public class LogWindowSink : ILogEventSink
{
    readonly ITextFormatter _textFormatter;
    readonly object _syncRoot = new object();

    public LogWindowSink(ITextFormatter textFormatter)
        => _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
        lock (_syncRoot) try { WindowsManager.GetWindow<LogsWindow>
                ("logs").Log(logEvent, _textFormatter); }
        catch { /* Ignore */ }
    }
}