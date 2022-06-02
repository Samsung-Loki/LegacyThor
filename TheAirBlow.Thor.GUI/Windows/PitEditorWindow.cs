// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using ImGuiNET;
using TheAirBlow.Thor.Enigma.PIT;

namespace TheAirBlow.Thor.GUI.Windows;

public class PitEditorWindow : Window
{
    private PitFile? _pit;
    
    /// <summary>
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "PIT Editor";
    
    /// <summary>
    /// Draw the Pit Editor window
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        if (ImGui.Begin("PIT Viewer", ref opened)) {
            if (ImGui.Button("Open"))
                WindowsManager.OpenFilePicker("Select .PIT file", "piteditor", ".pit");
            ImGui.SameLine();
            if (ImGui.Button("Close"))
                _pit = null;

            if (WindowsManager.FilePickerID == "piteditor") {
                _pit = new PitFile(WindowsManager.SelectedFilePickerPath);
                WindowsManager.ClearPicker();
            }

            if (_pit != null) {
                ImGui.SameLine();
                if (ImGui.Button(_pit.IsVersion2 ? "Force V1" : "Force V2"))
                    _pit.IsVersion2 = !_pit.IsVersion2;
                ImGui.SameLine();
                var version = _pit.IsVersion2 ? "2.0" : "1.0";
                var gang = string.IsNullOrEmpty(_pit.Header.GangName) ? "" : $"GANG Name: {_pit.Header.GangName} | ";
                var project = string.IsNullOrEmpty(_pit.Header.ProjectName) ? "" : $"Project Name: {_pit.Header.ProjectName} | ";
                ImGui.Text($"{gang}{project}Version {version}");
                
                if (ImGui.BeginTable("Partitions", 12, ImGuiTableFlags.Borders 
                                                       | ImGuiTableFlags.PreciseWidths 
                                                       | ImGuiTableFlags.SizingStretchProp)) {
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
                    ImGui.Text(_pit.IsVersion2 ? "Start Block" : "Unit Size (kB)");
                    ImGui.TableNextColumn();
                    ImGui.Text(_pit.IsVersion2 ? "Block Number" : "Unit Count");
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
        if (!opened) Close();
    }
}