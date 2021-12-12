using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Hreidmar.Enigma;
using Hreidmar.Enigma.Packets;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Hreidmar.GUI.Windows
{
    public class SessionWindow : Window
    {
        /// <summary>
        /// Show unsafe commands
        /// </summary>
        private bool _unsafeCommands;
        
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
            if (ImGui.Begin("Session", ImGuiWindowFlags.AlwaysAutoResize)) {
                var session = WindowsManager.GetWindow<DevicesWindow>("devices").Session;
                if (ImGui.Button("Begin Session")) {
                    try { session.BeginSession(); } 
                    catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                }
                
                if (ImGui.Button("End Session")) {
                    try { session.EndSession(); } 
                    catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                }
                
                if (ImGui.Button("Dispose")) {
                    try {
                        session.Dispose();
                        WindowsManager.GetWindow<DevicesWindow>("devices").Session = null;
                    } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); 
                        WindowsManager.GetWindow<DevicesWindow>("devices").Session = null; }
                }
                
                if (ImGui.Button("Reboot")) {
                    try { session.Reboot(); } 
                    catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                }
                
                if (ImGui.Button("Shutdown")) {
                    try { session.Shutdown(); } 
                    catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                }

                ImGui.Checkbox("Show unsafe commands", ref _unsafeCommands);
                if (_unsafeCommands) {
                    ImGui.Separator();
                    ImGui.Text("Untested commands");
                    if (ImGui.Button("Print sales code")) {
                        try { session.PrintSalesCode(); } 
                        catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                    }
                    ImGui.Separator();
                    ImGui.Text("Handshake-type packets");
                    if (ImGui.Button("ROOTING")) {
                        try {
                            session.Write(Encoding.ASCII.GetBytes("ROOTING"), 6000, out var sent);
                            var buf = new byte[1024];
                            session.Read(ref buf, 6000, out var read);
                            WindowsManager.GetWindow<LogsWindow>("logs").Log($"Wrote/read: {sent}/{read}");
                            File.WriteAllBytes("ROOTING.bin", buf);
                            WindowsManager.GetWindow<LogsWindow>("logs").Log($"Dump saved as ROOTING.bin");
                        } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                    }
                    if (ImGui.Button("SECCMD")) {
                        try {
                            session.Write(Encoding.ASCII.GetBytes("SECCMD"), 6000, out var sent);
                            var buf = new byte[1024];
                            session.Read(ref buf, 6000, out var read);
                            WindowsManager.GetWindow<LogsWindow>("logs").Log($"Wrote/read: {sent}/{read}");
                            File.WriteAllBytes("SECCMD.bin", buf);
                            WindowsManager.GetWindow<LogsWindow>("logs").Log($"Dump saved as SECCMD.bin");
                        } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                    }
                    ImGui.Separator();
                }

                if (WindowsManager.GetWindow<DevicesWindow>("devices").Session != null) {
                    ImGui.RadioButton("Session active", session.SessionBegan);
                    ImGui.RadioButton("Handshake done", session.HandshakeDone);
                    ImGui.RadioButton("T-Flash", session.TFlashEnabled);
                    if (!session.IsConnected()) {
                        try {
                            WindowsManager.GetWindow<LogsWindow>("logs").Log($"FATAL: Device disconnected!\n");
                            session.Dispose();
                            WindowsManager.GetWindow<DevicesWindow>("devices").Session = null;
                        } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); 
                            WindowsManager.GetWindow<DevicesWindow>("devices").Session = null; }
                    }
                }
                ImGui.End();
            }
        }
    }
}