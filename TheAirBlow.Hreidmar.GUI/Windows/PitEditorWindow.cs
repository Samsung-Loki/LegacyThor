// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using ImGuiNET;
using TheAirBlow.Hreidmar.Enigma.PIT;

namespace TheAirBlow.Hreidmar.GUI.Windows;

public class PitEditorWindow : Window
{
    private FilePicker? _picker;
    private bool _isPicking;
    private PitFile? _pit;
    
    /// <summary>
    /// Draw the Pit Editor window
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        if (ImGui.Begin("PIT Viewer", ref opened, ImGuiWindowFlags.AlwaysVerticalScrollbar)) {
            if (ImGui.Button("Open")) {
                _picker = FilePicker.GetFilePicker("piteditor", Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop), ".pit");
                _isPicking = true;
            }

            if (_pit != null) {
                ImGui.SameLine();
                if (ImGui.Button(_pit.IsVersion2 ? "Force V1" : "Force V2"))
                    _pit.IsVersion2 = !_pit.IsVersion2;
                ImGui.SameLine();
                var version = _pit.IsVersion2 ? "2.0" : "1.0";
                ImGui.Text($"GANG Name: {_pit.Header.GangName} | Project Name: {_pit.Header.ProjectName} | Version {version}");
                
                if (ImGui.BeginTable("Partitions", 12, ImGuiTableFlags.Borders | ImGuiTableFlags.PreciseWidths | ImGuiTableFlags.SizingStretchProp)) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Binary Type");
                    ImGui.TableNextColumn();
                    ImGui.Text("Device Type");
                    ImGui.TableNextColumn();
                    ImGui.Text("ID");
                    ImGui.TableNextColumn();
                    ImGui.Text(_pit.IsVersion2 ? "Partition Type" : "Attributes");
                    ImGui.TableNextColumn();
                    ImGui.Text(_pit.IsVersion2 ? "File System" : "Update Attributes");
                    ImGui.TableNextColumn();
                    ImGui.Text(_pit.IsVersion2 ? "Unit Size (kB)" : "Start Block");
                    ImGui.TableNextColumn();
                    ImGui.Text(_pit.IsVersion2 ? "Unit Count" : "Block Number");
                    ImGui.TableNextColumn();
                    ImGui.Text("File Offset");
                    ImGui.TableNextColumn();
                    ImGui.Text("File Size");
                    ImGui.TableNextColumn();
                    ImGui.Text("Partition Name");
                    ImGui.TableNextColumn();
                    ImGui.Text("File Name");
                    ImGui.TableNextColumn();
                    ImGui.Text("Delta Name");
                    foreach (var i in _pit.Entries) {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(i.BinaryType.ToString());
                        
                        ImGui.TableNextColumn();
                        ImGui.Text(i.DeviceType.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.Identifier.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.Attributes.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.UpdateAttributes.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.StartBlockOrCount.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.BlockCountOrNumber.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.FileOffset.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.FileSize.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(i.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(i.FileName);
                        ImGui.TableNextColumn();
                        ImGui.Text(i.DeltaName);
                    }
                    ImGui.EndTable();
                }
            }
            
            ImGui.End();
        }

        if (_isPicking)
        {
            var opened2 = true;
            if (ImGui.Begin("File Picker (PIT Editor)", ref opened2, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (_picker!.Draw()) {
                    Program.Logger.Information($"Trying to load PIT file from {_picker.SelectedFile}...");
                    _isPicking = false;
                    try { _pit = new(_picker.SelectedFile); }
                    catch (Exception e) { Program.Logger.Error(e, 
                        "Unable to parse selected PIT file!"); }
                    Program.Logger.Information("Successfully loaded the PIT file!");
                }
            }

            if (!opened2) _isPicking = false;
        }
        if (!opened) Close();
    }
}