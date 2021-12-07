using System;
using System.Collections.Generic;
using System.Linq;
using Hreidmar.Library;
using Hreidmar.Library.Packets;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Microsoft.Xna.Framework;
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

        private List<UsbDevice> _devices = new();
        private DeviceSession.OptionsClass _options = new();

        private void Refresh()
        {
            foreach (UsbRegistry device in UsbDevice.AllDevices) _devices.Add(device.Device);
            foreach (var device in _devices) {
                if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("Access denied!");
                if (device?.Info == null) continue;
                _devicesNames.Add(string.IsNullOrEmpty(device.Info.ProductString) ? "<null_product_name>" : device.Info.ProductString);
            }
        }

        private int _currentDeviceIndex = 0x04;
        private List<string> _devicesNames = new();
        private int _protocolVersionIndex;
            
        private UsbDevice _currentDevice {
            get {
                if (UsbDevice.AllDevices.Count <= _currentDeviceIndex) return null;
                return UsbDevice.AllDevices[_currentDeviceIndex].Device;
            }
        }

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

            base.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(114, 144, 154));
            _imGuiRenderer.BeforeLayout(gameTime);
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
            if (ImGui.Begin("Options", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Checkbox("Resume USB connection", ref _options.ResumeUsbConnection);
                ImGui.Checkbox("Resume session", ref _options.ResumeSession);
                ImGui.Checkbox("Automatic reboot", ref _options.AutoReboot);
                ImGui.Checkbox("Handshake on connect", ref _options.AutoHandshake);
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
                if (_currentDevice?.Info?.Descriptor?.VendorID != DeviceSession.SamsungKVid || !DeviceSession
                    .SamsungPids.ToList().Contains((int) _currentDevice?.Info?.Descriptor?.ProductID)) {
                    ImGui.Text("WARNING: This device is not a Hreidmar-compatible download mode Samsung device.");
                    ImGui.Text("WARNING: If it isn't, please refresh devices list.");
                }
                if (ImGui.Button("Refresh devices list"))
                    Refresh();
                if (ImGui.Button("Connect")) {
                    try { _session = new DeviceSession(_currentDevice, _options, (str) => { _loggingData += str + "\n"; } ); }
                    catch (Exception e) { _loggingData += $"Exception occured: {e}"; }
                }
            }
            ImGui.SetNextWindowSize(new Num.Vector2(400, 500));
            if (ImGui.Begin("Logs", ImGuiWindowFlags.HorizontalScrollbar)) {
                if (ImGui.Button("Clear"))
                    _loggingData = "";
                ImGui.Separator();
                ImGui.TextWrapped(_loggingData);
            }

            if (_session != null && ImGui.Begin("Session", ImGuiWindowFlags.AlwaysAutoResize)) {
                if (!_session.IsConnected())
                    ImGui.Text("WARNING: Your device disconnected!");
                
                if (ImGui.Button("Begin Session")) {
                    try { _session.BeginSession(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}"; }
                }
                
                if (ImGui.Button("End Session")) {
                    try { _session.EndSession(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}"; }
                }
                
                if (ImGui.Button("Dispose")) {
                    try {
                        _session.Dispose();
                        _session = null;
                    } catch (Exception e) { _loggingData += $"Exception occured: {e}"; _session = null; }
                }
                
                if (ImGui.Button("Reboot")) {
                    try { _session.Reboot(); } 
                    catch (Exception e) { _loggingData += $"Exception occured: {e}"; }
                }

                ImGui.RadioButton("Session active", _session.SessionBegan);
                ImGui.RadioButton("Handshake done", _session.HandshakeDone);
                ImGui.RadioButton("T-Flash", _session.TFlashEnabled);
            }
            _imGuiRenderer.AfterLayout();
            base.Draw(gameTime);
        }
    }
}
