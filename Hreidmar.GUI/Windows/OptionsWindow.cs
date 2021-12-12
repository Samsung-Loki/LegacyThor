using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hreidmar.Enigma;
using Hreidmar.Enigma.Packets;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Hreidmar.GUI.Windows
{
    public class OptionsWindow : Window
    {
        /// <summary>
        /// Options
        /// </summary>
        public readonly DeviceSession.OptionsClass Options = new();
        
        /// <summary>
        /// Protocol version
        /// </summary>
        private int _protocolVersionIndex = 0x04;

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
        /// Draw the Options window
        /// </summary>
        public override void Draw()
        {
            if (ImGui.Begin("Options", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Checkbox("Resume USB connection", ref Options.ResumeUsbConnection);
                ImGui.Checkbox("Resume session", ref Options.ResumeSession);
                ImGui.Checkbox("Automatic reboot", ref Options.AutoReboot);
                ImGui.Checkbox("T-Flash", ref Options.EnableTFlash);
                var enumValue = (ProtocolVersion) _protocolVersionIndex;
                ImGui.SliderInt("Protocol", ref _protocolVersionIndex, 0x03, 0x04, enumValue.ToString());
                Options.Protocol = enumValue;
                if (WindowsManager.GetWindow<DevicesWindow>("devices").Session != null) {
                    ImGui.Separator();
                    if (ImGui.Button("Apply")) {
                        try { WindowsManager.GetWindow<DevicesWindow>("devices").Session.ApplyChanges(Options); }
                        catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Unable to apply new options: {e.Message}!"); }
                    }
                }
                ImGui.End();
            }
        }
    }
}