using System;
using System.Numerics;
using ImGuiNET;

namespace Hreidmar.GUI.Windows
{
    public class PornWindow : Window
    {
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
            ImGui.SetNextWindowSize(new Vector2(200, 135));
            if (ImGui.Begin("Porn Window", ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoResize)) {
                ImGui.TextWrapped("Relax and Watch Porn while your Samsung Flashes!");
                ImGui.TextWrapped("- Otus9051#2183");
                ImGui.End();
            }
        }
    }
}