// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;
using ImGuiNET;
using Serilog.Events;
using Serilog.Formatting;
using TextCopy;

namespace TheAirBlow.Thor.GUI.Windows;

public class LogsWindow : Window
{
    /// <summary>
    /// Logs to draw
    /// </summary>
    private string _logs = "";

    /// <summary>
    /// How many warnings
    /// </summary>
    private int _warnings;

    /// <summary>
    /// How many errors
    /// </summary>
    private int _errors;

    /// <summary>
    /// Do autoscroll
    /// </summary>
    private bool _autoScroll;
    
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
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "Logs";

    /// <summary>
    /// Log a Serilog event
    /// </summary>
    /// <param name="_event">Event</param>
    /// <param name="formatter">Text formatter</param>
    public void Log(LogEvent _event, ITextFormatter formatter)
    {
        using var memory = new MemoryStream();
        using var stream = new StreamWriter(memory);
        formatter.Format(_event, stream);
        stream.Flush();
        _logs += Encoding.UTF8.GetString(
            memory.ToArray());
        switch (_event.Level) {
            case LogEventLevel.Fatal:
            case LogEventLevel.Error:
                _errors++;
                break;
            case LogEventLevel.Warning:
                _warnings++;
                break;
        }

        _autoScroll = true;
    }
    
    /// <summary>
    /// Draw the Logs window
    /// </summary>
    public override void Draw()
    {
        if (ImGui.Begin("Logs")) {

            if (ImGui.Button("Clear")) {
                _warnings = 0;
                _errors = 0;
                _logs = "";
            }
            ImGui.SameLine();
            if (ImGui.Button("Copy"))
                new Clipboard().SetText(_logs);
            ImGui.SameLine();
            ImGui.Text($"Errors: {_errors} | Warnings : {_warnings}");
            ImGui.Separator();
            if (ImGui.BeginChild(1)) {
                ImGui.TextUnformatted(_logs);
                if (_autoScroll) {
                    ImGui.SetScrollHereY(1);
                    _autoScroll = false;
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }
    }
}