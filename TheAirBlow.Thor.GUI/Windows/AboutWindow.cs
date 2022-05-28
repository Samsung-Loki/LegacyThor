// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ImGuiNET;

namespace TheAirBlow.Thor.GUI.Windows;

public class AboutWindow : Window
{
    /// <summary>
    /// Draw the About window
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        if (ImGui.Begin("About", ref opened, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text("Thpr is an open-source Odin/Heimdall alternative.");
            ImGui.Text("Licence: Mozilla Public License 2.0");
            ImGui.Text("GitHub: Samsung-Loki/Thor");
            ImGui.Separator();
            ImGui.Text($"Thor GUI is an ImGui.NET frontend.");
            ImGui.Text($"Enigma Engine is Odin protocol implementation.");
            ImGui.Separator();
            ImGui.Text($"Running Thor GUI version {typeof(Window).Assembly.GetName().Version}");
            ImGui.Text($"Running Enigma Engine version {typeof(Window).Assembly.GetName().Version}");
            ImGui.End();
        }
        if (!opened) Close();
    }
}
