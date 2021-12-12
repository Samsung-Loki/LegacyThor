using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using Hreidmar.Enigma;
using Hreidmar.Enigma.Packets;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Hreidmar.GUI.Windows
{
    public class PitWindow : Window
    {
        /// <summary>
        /// Unlock PIT flashing
        /// </summary>
        private bool _unlockPit;
        
        /// <summary>
        /// Progress value
        /// </summary>
        private float _value;
        
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
            if (ImGui.Begin("PIT", ImGuiWindowFlags.AlwaysAutoResize)) {
                var session = WindowsManager.GetWindow<DevicesWindow>("devices").Session;
                ImGui.ProgressBar(_value);
                if (ImGui.Button("Dump to \"dump.pit\"")) {
                    new Thread(() => { 
                        try {
                            File.WriteAllBytes("dump.pit", session.DumpPit((i, i1) => {
                                _value = i1 / 100 * i;
                            }));
                        } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Dumping failed: {e}"); }
                    }).Start();
                }
                if (_unlockPit) {
                    ImGui.InputText("PIT", ref WindowsManager.GetWindow<PitFlashSelectWindow>("pitselect").SelectedFile, 256);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse")) WindowsManager.Open("pitselect");
                    if (ImGui.Button("Repartition")) {
                        new Thread(() => {
                            try { session.FlashPit(File.ReadAllBytes(WindowsManager.GetWindow<PitFlashSelectWindow>("pitselect").SelectedFile)); } 
                            catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Repartition failed: {e}"); }
                        }).Start();
                    }
                } else {
                    ImGui.Text("WARNING: PIT flashing can cause critical damage to your device.");
                    ImGui.Text("WARNING: Proceed on your own risk.");
                    if (ImGui.Button("Unlock repartition")) _unlockPit = true;
                }
                ImGui.End();
            }
        }
    }
}