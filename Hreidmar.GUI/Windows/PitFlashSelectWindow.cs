using System.IO;
using ImGuiNET;
using Nez.ImGuiTools;

namespace Hreidmar.GUI.Windows
{
    public class PitFlashSelectWindow : Window
    {
        /// <summary>
        /// Selected file
        /// </summary>
        public string SelectedFile = "";
        
        /// <summary>
        /// Draw the Select file to flash window
        /// </summary>
        public override void Draw()
        {
            var opened = true;
            if (ImGui.Begin("Select PIT to flash", ref opened, ImGuiWindowFlags.Modal | ImGuiWindowFlags.AlwaysAutoResize)) {
                var picker = FilePicker.GetFilePicker(this, Directory.GetCurrentDirectory(), "pit");
                if (picker.Draw()) SelectedFile = picker.SelectedFile;
                ImGui.End();
            }
            if (!opened) Close();
        }
    }
}