// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;
using TheAirBlow.Hreidmar.GUI.Windows;

namespace TheAirBlow.Hreidmar.GUI;

public class SerilogLogWindow : TextWriter
{
    private LogsWindow _logs;

    public SerilogLogWindow()
        => _logs = WindowsManager.GetWindow<LogsWindow>("logs");

    public override void Write(char value)
        => _logs.Write(value);

    public override Encoding Encoding => Encoding.Default;
}