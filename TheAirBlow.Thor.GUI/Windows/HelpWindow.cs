// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ImGuiNET;

namespace TheAirBlow.Thor.GUI.Windows;

public class HelpWindow : Window
{
    /// <summary>
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "Help";
    
    /// <summary>
    /// Draw the Help window
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        if (ImGui.Begin("Help", ref opened, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text("In development!");
            ImGui.End();
        }
        if (!opened) Close();
    }
}