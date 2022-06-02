// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Numerics;
using ImGuiNET;

namespace TheAirBlow.Thor.GUI.Windows;

public class AboutWindow : Window
{
    /// <summary>
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "About Thor";
    
    /// <summary>
    /// Draw the About window
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        if (ImGui.Begin("About Thor", ref opened, ImGuiWindowFlags.AlwaysAutoResize)) {
            if (ImGui.BeginTable("Logo", 2)) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Image(MonoGameController.Instance.Logo, new Vector2(80, 80));
                ImGui.TableNextColumn();
                ImGui.Text("Thor is an open-source Odin/Heimdall alternative");
                ImGui.Text("Licence: Mozilla Public License 2.0");
                ImGui.Text("GitHub: Samsung-Loki/Thor");
                ImGui.EndTable();
            }
            ImGui.Separator();
            if (ImGui.BeginTable("Logo", 2)) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Image(MonoGameController.Instance.God, new Vector2(80, 80));
                ImGui.TableNextColumn();
                ImGui.Text("Coded from scratch by TheAirBlow");
                ImGui.Text($"Thor GUI is an ImGui.NET frontend, cuz why not");
                ImGui.Text($"Enigma Engine is a library, doing most of the stuff.");
                ImGui.EndTable();
            }
            ImGui.Separator();
            ImGui.Text($"Running Thor GUI version {typeof(Window).Assembly.GetName().Version}");
            ImGui.Text($"Running Enigma Engine version {typeof(Window).Assembly.GetName().Version}");
            ImGui.End();
        }
        if (!opened) Close();
    }
}
