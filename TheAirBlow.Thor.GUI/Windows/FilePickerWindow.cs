// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace TheAirBlow.Thor.GUI.Windows;

public class FilePickerWindow : Window
{
    /// <summary>
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "File Picker";
    
    /// <summary>
    /// The path selected
    /// </summary>
    public string? SelectedPath;

    /// <summary>
    /// Current file picker's ID
    /// </summary>
    public string ID;
    
    /// <summary>
    /// Window's title
    /// </summary>
    private string _title = null!;
    
    /// <summary>
    /// Extensions to show
    /// </summary>
    private List<string> _extensions = null!;
    
    /// <summary>
    /// Current directory
    /// </summary>
    private string _currentDir = null!;
    
    /// <summary>
    /// Directory/file picker
    /// </summary>
    private bool _directorySelect;

    /// <summary>
    /// Stuff you typed in the input box
    /// </summary>
    private string _inputBox = "";

    /// <summary>
    /// Directories to which you can get back by using an undo
    /// </summary>
    private List<string> _undoDirectories = null!;
    
    /// <summary>
    /// Directories to which you can get back by using an redo
    /// </summary>
    private List<string> _redoDirectories = null!;

    /// <summary>
    /// Selected directory/file
    /// </summary>
    private string _selected = "";

    /// <summary>
    /// Draw the File Picker
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        ImGui.SetNextWindowSize(new Vector2(400, 508));
        if (ImGui.Begin(_title, ref opened, ImGuiWindowFlags.NoResize)) {
            ImGui.PushItemWidth(384);
            ImGui.InputText("", ref _inputBox, 25565);
            if (ImGui.IsItemDeactivatedAfterEdit()) {
                if (File.Exists(_inputBox)) {
                    WindowsManager.Close("filepicker");
                    SelectedPath = _inputBox;
                } else if (Directory.Exists(_inputBox))
                    _currentDir = _inputBox.TrimEnd('/');
                else _inputBox = _currentDir;
            }

            if (ImGui.BeginChildFrame(1, new Vector2(384, 400))) {
                var di = new DirectoryInfo(_currentDir);
                if (di.Exists) {
                    if (di.Parent != null) {
                        ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);
                        if (ImGui.Selectable("../", _selected == "../"))
                            _selected = "../";

                        ImGui.PopStyleColor();
                    }
            
                    var fileSystemEntries = GetFileSystemEntries(di.FullName);
                    foreach (var fse in fileSystemEntries) {
                        if (Directory.Exists(fse)) {
                            var name = Path.GetFileName(fse);
                            ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);
                            if (ImGui.Selectable(name + "/", _selected == name + "/")) {
                                _selected = name + "/";
                                if (_directorySelect) SelectedPath = _currentDir + "/" + _selected.TrimEnd('/');
                            }

                            ImGui.PopStyleColor();
                        } else {
                            var name = Path.GetFileName(fse);
                            var isSelected = SelectedPath == fse;
                            if (ImGui.Selectable(name, isSelected)) {
                                _selected = fse;
                                SelectedPath = fse;
                            }
                        }
                    }
                }
                
                if (ImGui.IsMouseDoubleClicked(0)) {
                    if (_selected == "../") {
                        _undoDirectories.Add(_currentDir);
                        _currentDir = di.Parent!.FullName;
                        _inputBox = _currentDir;
                        SelectedPath = null;
                        _selected = "";
                    } else if (_selected != "") {
                        if (!_selected.EndsWith("/") && !_directorySelect) {
                            Program.Logger.Information($"Selected {SelectedPath}");
                            WindowsManager.Close("filepicker");
                        } else {
                            _undoDirectories.Add(_currentDir);
                            _currentDir = _currentDir + "/" + _selected.TrimEnd('/');
                            _inputBox = _currentDir;
                            if (_directorySelect) SelectedPath = null;
                        }
                    }
                }
                
                ImGui.EndChildFrame();
            }

            if (ImGui.Button("Cancel")) {
                Program.Logger.Information($"File picker closed by cancel button");
                WindowsManager.Close("filepicker");
                SelectedPath = null; ID = "";
            }
            
            ImGui.SameLine();
            ImGui.BeginDisabled(_undoDirectories.Count == 0);
            if (ImGui.Button("Undo")) {
                _redoDirectories.Add(_currentDir);
                _currentDir = _undoDirectories.Last();
                _inputBox = _currentDir;
                _undoDirectories.RemoveAt(_undoDirectories.Count - 1);
                _selected = "";
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.BeginDisabled(_redoDirectories.Count == 0);
            if (ImGui.Button("Redo")) {
                _undoDirectories.Add(_currentDir);
                _currentDir = _redoDirectories.Last();
                _inputBox = _currentDir;
                _redoDirectories.RemoveAt(_redoDirectories.Count - 1);
                _selected = "";
            }
            ImGui.EndDisabled();
            
            ImGui.BeginDisabled(SelectedPath == null);
            ImGui.SameLine(ImGui.GetWindowWidth()-61);
            if (ImGui.Button("Select")) {
                Program.Logger.Information($"Selected {SelectedPath}");
                WindowsManager.Close("filepicker");
            }
            ImGui.EndDisabled();
            ImGui.End();
        }

        if (!opened) {
            Program.Logger.Information($"File picker closed by close button");
            SelectedPath = null; ID = ""; Close();
        }
    }
    
    /// <summary>
    /// Get files/directories to render
    /// </summary>
    /// <param name="fullName">Path to directory</param>
    /// <returns>Files and directories</returns>
    private List<string> GetFileSystemEntries(string fullName) {
        var files = new List<string>();
        var dirs = new List<string>();

        try {
            foreach (var fse in Directory.GetFileSystemEntries(fullName, "")) {
                if (Directory.Exists(fse)) dirs.Add(fse);
                else if (!_directorySelect) {
                    if (_extensions.Count != 0) {
                        var ext = Path.GetExtension(fse);
                        if (_extensions.Contains(ext))
                            files.Add(fse);
                    } else files.Add(fse);
                }
            }
        } catch (UnauthorizedAccessException e) {
            WindowsManager.ShowPopup("Unable to open directory",
                "Access denied, please run as admin!");
            _currentDir = _undoDirectories.Last();
            _inputBox = _currentDir; _selected = "";
            _undoDirectories.RemoveAt(_undoDirectories.Count - 1);
        }

        var ret = new List<string>(dirs);
        ret.AddRange(files);
        ret.Sort();
        return ret;
    }

    /// <summary>
    /// Make the File Picker draw and do it's thing
    /// </summary>
    /// <param name="title">Window's Title</param>
    /// <param name="extensions">Extensions split by |</param>
    /// <param name="id">ID, you should get why it's needed</param>
    /// <param name="directory">Select directory (true) or file (false)</param>
    public void CreateNew(string title, string id, string extensions = "", bool directory = false)
    {
        Program.Logger.Information($"Created a new filepicker {id}");
        _title = title; ID = id;
        _extensions = new();
        _undoDirectories = new();
        _redoDirectories = new();
        _currentDir = Environment.GetFolderPath(
            Environment.SpecialFolder.Desktop);
        _inputBox = _currentDir;
        foreach (var i in extensions.Split("|", 
                     StringSplitOptions.RemoveEmptyEntries))
            _extensions.Add(i);
        _directorySelect = directory;
        WindowsManager.Open("filepicker");
    }
}