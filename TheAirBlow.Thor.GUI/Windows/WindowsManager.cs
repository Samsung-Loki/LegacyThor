// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    private static Action? _popupAction;

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
    /// Make the File Picker draw and do it's thing
    /// </summary>
    /// <param name="title">Window's Title</param>
    /// <param name="extensions">Extensions split by |</param>
    /// <param name="id">ID, you should get why it's needed</param>
    /// <param name="directory">Select directory (true) or file (false)</param>
    public static void OpenFilePicker(string title, string id, string extensions = "", bool directory = false)
        => ((FilePickerWindow) _windows["filepicker"]).CreateNew(title, id, extensions, directory);

    /// <summary>
    /// File Picker's selected pack
    /// </summary>
    public static string? SelectedFilePickerPath => 
        _windows["filepicker"].IsOpened() ? null : 
            ((FilePickerWindow) _windows["filepicker"]).SelectedPath;
    
    /// <summary>
    /// File Picker's ID
    /// </summary>
    public static string? FilePickerID =>
        _windows["filepicker"].IsOpened() ? null : 
        ((FilePickerWindow) _windows["filepicker"]).ID;

    /// <summary>
    /// Clear File Picker's ID
    /// </summary>
    public static void ClearPicker()
        => ((FilePickerWindow) _windows["filepicker"]).ID = "";

    /// <summary>
    /// Open a window
    /// </summary>
    /// <param name="name">Name</param>
    public static void Open(string name)
        => _windows[name].Open();

    /// <summary>
    /// Close a window
    /// </summary>
    /// <param name="name">Name</param>
    public static void Close(string name)
        => _windows[name].Close();

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
    /// Show a popup
    /// </summary>
    /// <param name="title">Title</param>
    /// <param name="message">Message</param>
    public static void ShowPopup(string title, string message)
        => _popupAction = () => {
            var open = true;
            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), 
                ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowFocus();
            if (ImGui.Begin(title, ref open, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text(message);
                if (ImGui.Button("OK", new Vector2(ImGui.GetWindowWidth() - 18, 30)))
                    open = false;
                ImGui.End();
            }

            if (!open) _popupAction = null;
        };

    /// <summary>
    /// Draw all windows
    /// </summary>
    public static void Draw()
    {
        foreach (var window in _windows) {
            try {
                if (window.Value.IsOpened()) window.Value.Draw();
            } catch (Exception e) {
                Program.Logger.Error(e, $"Exception in window {window.Value.VisibleName}");
                ShowPopup($"{window.Value.VisibleName} window crashed",
                    "It is, uh, very much not intended to happen!\n" +
                    "That window was completely disabled to prevent crashing.\n" +
                    "Please report this issue on Thor's GitHub including logs.");
                _windows.Remove(window.Key);
            }
        }
        if (_popupAction != null) _popupAction();
    }
}