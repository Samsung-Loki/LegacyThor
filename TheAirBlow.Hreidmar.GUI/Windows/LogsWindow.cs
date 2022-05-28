// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace TheAirBlow.Hreidmar.GUI.Windows;

public class LogsWindow : Window
{
    /// <summary>
    /// Logging data
    /// </summary>
    private string _loggingData = "";

    /// <summary>
    /// Write to the logs
    /// </summary>
    /// <param name="str">Character</param>
    public void Write(char str)
        => _loggingData += str;

    /// <summary>
    /// Write to the logs
    /// </summary>
    /// <param name="str">String</param>
    public void WriteLine(string str)
        => _loggingData += $"{str}\n";
        
    /// <summary>
    /// Is the window opened
    /// </summary>
    /// <returns>Always true</returns>
    public override bool IsOpened()
        => true;

    /// <summary>
    /// Open the window (always throws an exception)
    /// </summary>
    /// <exception cref="Exception">Cannot be opened</exception>
    public override void Open()
        => throw new Exception("This window cannot be opened!");
        
    /// <summary>
    /// Close the window (always throws an exception)
    /// </summary>
    /// <exception cref="Exception">Cannot be closed</exception>
    public override void Close()
        => throw new Exception("This window cannot be closed!");

    /// <summary>
    /// Draw the logs window
    /// </summary>
    public override void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(500, 500));
        if (ImGui.Begin("Logs", ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoResize)) {
            if (ImGui.Button("Clear"))
                _loggingData = "";
            ImGui.Separator();
            ImGui.TextWrapped(_loggingData);
            ImGui.End();
        }
    }
}