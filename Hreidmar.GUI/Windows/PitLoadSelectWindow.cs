using System;
using System.IO;
using Hreidmar.Enigma.PIT;
using ImGuiNET;
using Nez.ImGuiTools;

namespace Hreidmar.GUI.Windows
{
    public class PitLoadSelectWindow : Window
    {
        /// <summary>
        /// Selected file
        /// </summary>
        public PitData PitData;
        
        /// <summary>
        /// Draw the Select PIT to load window
        /// </summary>
        public override void Draw()
        {
            var opened = true;
            if (ImGui.Begin("Select PIT file to load", ref opened, ImGuiWindowFlags.Modal | ImGuiWindowFlags.AlwaysAutoResize)) {
                var picker = FilePicker.GetFilePicker(this, Directory.GetCurrentDirectory());
                if (picker.Draw()) {
                    try { PitData = PitData.FromFile(picker.SelectedFile); } 
                    catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Unable to load PIT: {e.Message}!"); }
                }
                ImGui.End();
            }
            if (!opened) Close();
        }
    }
}