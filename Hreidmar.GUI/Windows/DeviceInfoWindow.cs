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
    public class DeviceInfoWindow : Window
    {
        /// <summary>
        /// Is the window opened
        /// </summary>
        /// <returns>Always true</returns>
        public override bool IsOpened()
            => WindowsManager.GetWindow<DevicesWindow>("devices").Session != null;

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
        /// Draw the Device Info window
        /// </summary>
        public override void Draw()
        {
            if (ImGui.Begin("Device Info", ImGuiWindowFlags.AlwaysAutoResize)) {
                var session = WindowsManager.GetWindow<DevicesWindow>("devices").Session;
                var model = session.Information.ContainsKey("MODEL") ? session.Information["MODEL"] : "Empty";
                var salesCode = session.Information.ContainsKey("SALES") ? session.Information["SALES"] : "Empty";
                var firmware = session.Information.ContainsKey("VER") ? session.Information["VER"] : "Empty";
                var did = session.Information.ContainsKey("DID") ? session.Information["DID"] : "Empty";
                ImGui.Text($"Model: {model}");
                ImGui.Text($"Region: {salesCode}");
                ImGui.Text($"Firmware: {firmware}");
                ImGui.Text($"DID: {did}");
                ImGui.End();
            }
        }
    }
}