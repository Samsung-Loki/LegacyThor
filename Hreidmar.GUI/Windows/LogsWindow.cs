using System;
using System.Numerics;
using ImGuiNET;

namespace Hreidmar.GUI.Windows
{
    public class LogsWindow : Window
    {
        /// <summary>
        /// Logging data
        /// </summary>
        private string _loggingData = "";

        /// <summary>
        /// Write to the logs
        /// </summary>
        /// <param name="str">String</param>
        public void Log(string str)
            => _loggingData += $"{str}\n";
        
        /// <summary>
        /// Is the window opened
        /// </summary>
        /// <returns>Always true</returns>
        public override bool IsOpened()
            => true;

        /// <summary>
        /// Open the window (always throws an exception)
        /// </summary>
        /// <exception cref="Exception">Cannot be opened</exception>
        public override void Open()
            => throw new Exception("This window cannot be opened!");
        
        /// <summary>
        /// Close the window (always throws an exception)
        /// </summary>
        /// <exception cref="Exception">Cannot be closed</exception>
        public override void Close()
            => throw new Exception("This window cannot be closed!");

        /// <summary>
        /// Draw the logs window
        /// </summary>
        public override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(500, 500));
            if (ImGui.Begin("Logs", ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoResize)) {
                if (ImGui.Button("Clear"))
                    _loggingData = "";
                ImGui.Separator();
                ImGui.TextWrapped(_loggingData);
                ImGui.End();
            }
        }
    }
}