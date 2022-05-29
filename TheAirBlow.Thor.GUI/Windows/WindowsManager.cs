// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace TheAirBlow.Thor.GUI.Windows;

/// <summary>
/// Hreidmar GUI windows manager
/// </summary>
public static class WindowsManager
{
    /// <summary>
    /// All windows
    /// </summary>
    private static readonly Dictionary<string, Window> _windows = new();
    
    /// <summary>
    /// Popup to show
    /// </summary>
    private static Action? _popupAction = null;
    
    /// <summary>
    /// Random instance
    /// </summary>
    private static readonly Random _random = new Random();

    /// <summary>
    /// Add a window
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="window">Window</param>
    public static void Add(string name, Window window)
        => _windows.Add(name, window);

    /// <summary>
    /// Remove a window
    /// </summary>
    /// <param name="name">Name</param>
    public static void Remove(string name)
    {
        Program.Logger.Information($"Removed window {name}");
        _windows.Remove(name);
    }

    /// <summary>
    /// Open a window
    /// </summary>
    /// <param name="name">Name</param>
    public static void Open(string name)
    {
        Program.Logger.Information($"Opened window {name}");
        _windows[name].Open();
    }

    /// <summary>
    /// Close a window
    /// </summary>
    /// <param name="name">Name</param>
    public static void Close(string name)
    {
        Program.Logger.Information($"Closed window {name}");
        _windows[name].Close();
    }

    /// <summary>
    /// Is window opened
    /// </summary>
    /// <param name="name">name</param>
    /// <returns>Value</returns>
    public static bool IsOpened(string name)
        => _windows[name].IsOpened();

    /// <summary>
    /// Get a window by name of type
    /// </summary>
    /// <param name="name">Name</param>
    /// <typeparam name="T">Type</typeparam>
    /// <exception cref="Exception">Requested Window type is not an instance of Window</exception>
    /// <returns>Window</returns>
    public static T GetWindow<T>(string name)
        => (T)(object)_windows[name];

    /// <summary>
    /// Generate random string
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Random string</returns>
    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static void ShowPopup(string title, string message)
        => _popupAction = () =>
        {
            var open = true;
            if (ImGui.Begin(title, ref open, ImGuiWindowFlags.Popup | ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text(message);
                if (ImGui.Button("OK"))
                    open = false;
            }

            if (open == false) _popupAction = null;
        };

    /// <summary>
    /// Draw all windows
    /// </summary>
    public static void Draw()
    {
        foreach (var window in _windows)
            if (window.Value.IsOpened()) window.Value.Draw();
        if (_popupAction != null) _popupAction();
    }
}