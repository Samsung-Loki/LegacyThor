using System;
using System.Numerics;
using ImGuiNET;

namespace Hreidmar.GUI.Windows
{
    public class HelpWindow : Window
    {
        /// <summary>
        /// Draw the Help window
        /// </summary>
        public override void Draw()
        {
            var opened = true;
            if (ImGui.Begin("Help", ref opened, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text("Session window:");
                ImGui.Text("You can control your current session here.");
                ImGui.Text("Check \"Show unsafe commands\" to unlock untested commands.");
                ImGui.Separator();
                ImGui.Text("Device Info window:");
                ImGui.Text("Here is all device information fetched using DVIF command.");
                ImGui.Separator();
                ImGui.Text("Device window:");
                ImGui.Text("Here is all USB devices detected.");
                ImGui.Text("Select one of them and hit \"Connect\".");
                ImGui.Separator();
                ImGui.Text("Logs window:");
                ImGui.Text("Here is all device information fetched using DVIF command.");
                ImGui.Separator();
                ImGui.Text("PIT window:");
                ImGui.Text("Here you can manage your device's PIT.");
                ImGui.Text("You can dump your current PIT, or flash/repartition it.");
                ImGui.Text("You need to hit \"Unlock repartition\" for flashing.");
                ImGui.Separator();
                ImGui.Text("Options:");
                ImGui.Text("Here you can control current/new session's settings.");
                ImGui.Text("Not all options can be changed on \"Apply\"!");
                ImGui.End();
            }
            if (!opened) Close();
        }
    }
}