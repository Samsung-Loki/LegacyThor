using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Hreidmar.Library;
using Hreidmar.Library.Packets;
using Hreidmar.Library.Packets.Inbound;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.DeviceNotify.Linux;
using LibUsbDotNet.Main;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.ImGuiTools;
using Num = System.Numerics;

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0044 // Add readonly modifier

namespace Hreidmar.GUI
{
    /// <summary>
    /// Simple FNA + ImGui example
    /// </summary>
    public class MonoGameController : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private DeviceSession _session = null;
        private string _loggingData = "";

        private List<UsbRegistry> _devices = new();
        private DeviceSession.OptionsClass _options = new();

        private void Refresh()
        {
            _devices.Clear();
            _devicesNames.Clear();
            foreach (UsbRegistry device in UsbDevice.AllDevices) _devices.Add(device);
            foreach (var device in _devices) {
                if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("Access denied!");
                if (device?.Device.Info == null) continue;
                _devicesNames.Add(device.FullName);
            }
        }

        private int _currentDeviceIndex = 0x04;
        private List<string> _devicesNames = new();
        private int _protocolVersionIndex;

        private UsbRegistry _currentDevice {
            get {
                if (UsbDevice.AllDevices.Count <= _currentDeviceIndex) return null;
                return UsbDevice.AllDevices[_currentDeviceIndex];
            }
        }
        
        private bool _unsafeCommands;
        private bool _unlockPit;

        private long _maxValue = 0;
        private long _value = 0;

        public MonoGameController()
        {
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1024,
                PreferredBackBufferHeight = 768,
                PreferMultiSampling = true
            };

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            Window.AllowUserResizing = true;
            Refresh();

            base.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(114, 144, 154));
            _imGuiRenderer.BeforeLayout(gameTime);
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
            if (_session != null && ImGui.Begin("Device Info", ImGuiWindowFlags.AlwaysAutoResize)) {
                var model = _session.Information.ContainsKey("MODEL") ? _session.Information["MODEL"] : "Empty";
                var salesCode = _session.Information.ContainsKey("SALES") ? _session.Information["SALES"] : "Empty";
                var firmware = _session.Information.ContainsKey("VER") ? _session.Information["VER"] : "Empty";
                var did = _session.Information.ContainsKey("DID") ? _session.Information["DID"] : "Empty";
                ImGui.Text($"Model: {model}");
                ImGui.Text($"Region: {salesCode}");
                ImGui.Text($"Firmware: {firmware}");
                ImGui.Text($"DID: {did}");
            }
            if (ImGui.Begin("Options", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Checkbox("Resume USB connection", ref _options.ResumeUsbConnection);
                ImGui.Checkbox("Resume session", ref _options.ResumeSession);
                ImGui.Checkbox("Automatic reboot", ref _options.AutoReboot);
                ImGui.Checkbox("T-Flash", ref _options.EnableTFlash);
                var enumValue = (ProtocolVersion) _protocolVersionIndex;
                ImGui.SliderInt("Protocol", ref _protocolVersionIndex, 0x03, 0x04, enumValue.ToString());
                _options.Protocol = enumValue;
                if (_session != null) {
                    ImGui.Separator();
                    if (ImGui.Button("Apply")) {
                        try { _session.ApplyChanges(_options); }
                        catch (Exception e) { _loggingData += $"Unable to apply new options: {e.Message}!"; }
                    }
                }
            }
            if (ImGui.Begin("Device", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Combo("Select device", ref _currentDeviceIndex, _devicesNames.ToArray(), _devicesNames.Count);
                try {
                    if (_currentDevice.Vid != DeviceSession.SamsungKVid || !DeviceSession
                        .SamsungPids.ToList().Contains(_currentDevice.Pid)) {
                        ImGui.Text("WARNING: This device is not a Hreidmar-compatible download mode Samsung device.");
                        ImGui.Text("WARNING: If it isn't, please refresh devices list.");
                    }
                } catch { Refresh(); }
                if (ImGui.Button("Refresh devices list"))
                    Refresh();
                if (ImGui.Button("Connect")) {
                    try { _session = new DeviceSession(_currentDevice, _options, (str) => { _loggingData += str + "\n"; } ); }
                    catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                }
            }
            ImGui.SetNextWindowSize(new Num.Vector2(400, 500));
            if (ImGui.Begin("Logs", ImGuiWindowFlags.HorizontalScrollbar)) {
                if (ImGui.Button("Clear"))
                    _loggingData = "";
                ImGui.Separator();
                ImGui.TextWrapped(_loggingData);
            }

            if (_session != null && ImGui.Begin("PIT", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.ProgressBar(_value, new Num.Vector2(-1, _maxValue));
                if (ImGui.Button("Dump to \"dump.pit\"")) {
                    new Thread(() => { File.WriteAllBytes("dump.pit", _session.DumpPit((i, i1) => {
                            _value = i;
                            _maxValue = i1;
                        }));
                    }).Start();
                }
                if (!_unlockPit) {
                    if (ImGui.Button("Flash PIT/repartition")) {
                        var picker = FilePicker.GetFilePicker(this, Directory.GetCurrentDirectory());
                        if (picker.Draw()) _session.FlashPit(File.ReadAllBytes(picker.SelectedFile));
                    }
                } else {
                    ImGui.Text("WARNING: PIT flashing can cause critical damage to your device.");
                    ImGui.Text("WARNING: Proceed on your own risk.");
                    if (ImGui.Button("Unlock repartition")) _unlockPit = true;
                }
            }

            if (_session != null && ImGui.Begin("Session", ImGuiWindowFlags.AlwaysAutoResize)) {
                if (ImGui.Button("Begin Session")) {
                    try { _session.BeginSession(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                }
                
                if (ImGui.Button("End Session")) {
                    try { _session.EndSession(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                }
                
                if (ImGui.Button("Dispose")) {
                    try {
                        _session.Dispose();
                        _session = null;
                    } catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; _session = null; }
                    Refresh();
                }
                
                if (ImGui.Button("Reboot")) {
                    try { _session.Reboot(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                }
                
                if (ImGui.Button("Shutdown")) {
                    try { _session.Shutdown(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                }

                ImGui.Checkbox("Show unsafe commands", ref _unsafeCommands);
                if (_unsafeCommands) {
                    ImGui.Separator();
                    ImGui.Text("Untested commands");
                    if (ImGui.Button("Print sales code")) {
                        try { _session.PrintSalesCode(); } 
                        catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                    }
                    ImGui.Separator();
                    ImGui.Text("Handshake-type packets");
                    {
                        if (ImGui.Button("ROOTING")) {
                            try {
                                _session.Write(Encoding.ASCII.GetBytes("ROOTING"), 6000, out var sent);
                                var buf = new byte[1024];
                                _session.Read(ref buf, 6000, out var read);
                                _loggingData += $"Wrote/read: {sent}/{read}\n";
                                File.WriteAllBytes("ROOTING.bin", buf);
                                _loggingData += $"Dump saved as ROOTING.bin\n";
                            } catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                        }
                        if (ImGui.Button("SECCMD")) {
                            try {
                                _session.Write(Encoding.ASCII.GetBytes("SECCMD"), 6000, out var sent);
                                var buf = new byte[1024];
                                _session.Read(ref buf, 6000, out var read);
                                _loggingData += $"Wrote/read: {sent}/{read}\n";
                                File.WriteAllBytes("SECCMD.bin", buf);
                                _loggingData += $"Dump saved as SECCMD.bin\n";
                            } catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; }
                        }
                    }
                    ImGui.Separator();
                }

                if (_session != null) {
                    ImGui.RadioButton("Session active", _session.SessionBegan);
                    ImGui.RadioButton("Handshake done", _session.HandshakeDone);
                    ImGui.RadioButton("T-Flash", _session.TFlashEnabled);
                    if (!_session.IsConnected()) {
                        try {
                            _loggingData += $"\n\nFATAL: Device disconnected!\n\n";
                            _session.Dispose();
                            _session = null;
                        } catch (Exception e) { _loggingData += $"Exception occured: {e}\n"; _session = null; }
                        Refresh();
                    }
                }
            }
            _imGuiRenderer.AfterLayout();
            base.Draw(gameTime);
        }
    }
}
