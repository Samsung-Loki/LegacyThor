// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Numerics;
using ImGuiNET;

namespace TheAirBlow.Thor.GUI.Windows;

public class FaqWindow : Window
{
    /// <summary>
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "FAQ";
    
    /// <summary>
    /// Draw the FAQ window
    /// </summary>
    public override void Draw()
    {
        var opened = true;
        if (ImGui.Begin("FAQ", ref opened, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text("Q: An error occured, what I need to do?");
            ImGui.Text("A: If you are sure that nothing happened with the USB, " +
                       "please report it on Thor's GitHub.");
            ImGui.Separator();
            ImGui.Text("Q: Nothing happened after <something>, what I need to do?");
            ImGui.Text("A: Please check the logs. You'll have error information here.");
            ImGui.End();
        }
        if (!opened) Close();
    }
}