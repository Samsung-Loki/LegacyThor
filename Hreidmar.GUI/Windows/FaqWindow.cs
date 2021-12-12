using System;
using System.Numerics;
using ImGuiNET;

namespace Hreidmar.GUI.Windows
{
    public class FaqWindow : Window
    {
        /// <summary>
        /// Draw the FAQ window
        /// </summary>
        public override void Draw()
        {
            var opened = true;
            if (ImGui.Begin("FAQ", ref opened, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text("Q: What does \"DVIF!!\" mean?");
                ImGui.Text("A: DVIF is used to get your device's info. Ignore it.");
                ImGui.Separator();
                ImGui.Text("Q: An error occured, what I need to do?");
                ImGui.Text("A: If you are sure that nothing happened with the USB, " +
                                  "please report it on Hreidmar's GitHub.");
                ImGui.Separator();
                ImGui.Text("Q: Nothing happened after <something>, what I need to do?");
                ImGui.Text("A: Please check the logs. You'll have error information here.");
                ImGui.End();
            }
            if (!opened) Close();
        }
    }
}