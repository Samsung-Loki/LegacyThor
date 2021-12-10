using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Hreidmar.Library;
using Hreidmar.Library.Packets;
using Hreidmar.Library.Packets.Inbound;
using Hreidmar.Library.PIT;
using ImGuiNET;
using K4os.Compression.LZ4.Streams;
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
        private DeviceSession _session;
        private string _loggingData = "";
        
        private readonly DeviceSession.OptionsClass _options = new();
        private static int _lastCount;

        private static void Refresh()
        {
            if (_lastCount == UsbDevice.AllDevices.Count) return;
            _devicesNames.Clear();
            var tmp = new List<string>();
            foreach (UsbRegistry device in UsbDevice.AllDevices) {
                if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("Access denied!");
                if (device?.Device.Info == null) continue;
                tmp.Add(string.IsNullOrEmpty(device.FullName) ? $"Unnamed device (0x{device.Vid:X2}/0x{device.Pid:X2})" : device.FullName);
            }

            _devicesNames = tmp;
            _lastCount = UsbDevice.AllDevices.Count;
        }

        private int _currentDeviceIndex;
        private static List<string> _devicesNames = new();
        private int _protocolVersionIndex = 0x04;

        private UsbRegistry CurrentDevice {
            get {
                if (UsbDevice.AllDevices.Count <= _currentDeviceIndex) return null;
                return UsbDevice.AllDevices[_currentDeviceIndex];
            }
        }
        
        private bool _unsafeCommands;
        private bool _unlockPit;

        private float _value;
        private static System.Timers.Timer _timer = new();
        private ImFontPtr _font;
        
        private bool _showAbout;
        private bool _showHelp;
        private bool _showFaq;

        private PitData _pit;
        private string _pitFileToFlash;
        private int _currentPitEntry;
        private string _selectedFile = "";
        private Dictionary<string, PitEntry> Partitions = new();
        private List<PitEntry> AvailablePartitions;
        private int _partitionToDelete;
        
        private float _flashValue;
        private float _flashMax;

        private bool _showFileFlashSelect;
        private bool _showLoadPitSelect;
        private bool _showFlashPitSelect;

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
            _font = ImGui.GetIO().Fonts.AddFontFromFileTTF("Karla-Regular.ttf", 20);
            _imGuiRenderer.RebuildFontAtlas();

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            Window.AllowUserResizing = true;
            _timer.Interval = 100;
            _timer.Start();
            _timer.Elapsed += (_, _) => Refresh();

            base.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(114, 144, 154));
            _imGuiRenderer.BeforeLayout(gameTime);
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
            ImGui.PushFont(_font);
            if (_showFileFlashSelect) {
                if (ImGui.Begin("Select file to flash", ref _showFileFlashSelect)) {
                    var picker = FilePicker.GetFilePicker(this, Directory.GetCurrentDirectory());
                    if (picker.Draw()) _selectedFile = picker.SelectedFile;
                    ImGui.End();
                }
            }
            if (_showFlashPitSelect) {
                if (ImGui.Begin("Select PIT file to flash", ref _showFlashPitSelect)) {
                    var picker = FilePicker.GetFilePicker(this, Directory.GetCurrentDirectory());
                    if (picker.Draw()) _pitFileToFlash = picker.SelectedFile;
                    ImGui.End();
                }
            }
            if (_showLoadPitSelect) {
                if (ImGui.Begin("Select PIT file to load", ref _showLoadPitSelect)) {
                    var picker = FilePicker.GetFilePicker(this, Directory.GetCurrentDirectory());
                    if (picker.Draw()) {
                        try {
                            _pit = PitData.FromFile(picker.SelectedFile); 
                            AvailablePartitions = _pit.Entries;
                        } catch (Exception e) { _loggingData += $"Unable to load PIT: {e.Message}!"; }
                    }
                    ImGui.End();
                }
            }
            if (ImGui.BeginMainMenuBar()) {
                if (ImGui.BeginMenu("Help")) {
                    if (ImGui.MenuItem("About"))
                        _showAbout = true;
                    if (ImGui.MenuItem("Help"))
                        _showHelp = true;
                    if (ImGui.MenuItem("FAQ"))
                        _showFaq = true;
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
            if (_showFaq) {
                if (ImGui.Begin("About", ref _showFaq, ImGuiWindowFlags.AlwaysAutoResize)) {
                    ImGui.TextWrapped("Q: What does \"DVIF!!\" mean?");
                    ImGui.TextWrapped("A: DVIF is used to get your device's info. Ignore it.");
                    ImGui.Separator();
                    ImGui.TextWrapped("Q: An error occured, what I need to do?");
                    ImGui.TextWrapped("A: If you are sure that nothing happened with the USB, " +
                                      "please report it on Hreidmar's GitHub.");
                    ImGui.Separator();
                    ImGui.TextWrapped("Q: Nothing happened after <something>, what I need to do?");
                    ImGui.TextWrapped("A: Please check the logs. You'll have error information here.");
                    ImGui.End();
                }
            }
            if (_showHelp) {
                if (ImGui.Begin("Help", ref _showHelp, ImGuiWindowFlags.AlwaysAutoResize)) {
                    ImGui.Text("Session window:");
                    ImGui.Text("You can control your current session here.");
                    ImGui.Text("Check \"Show unsafe commands\" to unlock untested commands.");
                    ImGui.Separator();
                    ImGui.Text("Device Info window:");
                    ImGui.Text("Here is all device information fetched using DVIF command.");
                    ImGui.Separator();
                    ImGui.Text("Device window:");
                    ImGui.Text("Here is all USB devices detected.");
                    ImGui.Text("Select one of them and hit \"Connect\".");
                    ImGui.Separator();
                    ImGui.Text("Logs window:");
                    ImGui.Text("Here is all device information fetched using DVIF command.");
                    ImGui.Separator();
                    ImGui.Text("PIT window:");
                    ImGui.Text("Here you can manage your device's PIT.");
                    ImGui.Text("You can dump your current PIT, or flash/repartition it.");
                    ImGui.Text("You need to hit \"Unlock repartition\" for flashing.");
                    ImGui.Separator();
                    ImGui.Text("Options:");
                    ImGui.Text("Here you can control current/new session's settings.");
                    ImGui.Text("Not all options can be changed on \"Apply\"!");
                    ImGui.End();
                }
            }

            if (_showAbout) {
                if (ImGui.Begin("About", ref _showAbout, ImGuiWindowFlags.AlwaysAutoResize)) {
                    ImGui.Text("Hreidmar is an open-source Odin/Heimdall alternative.");
                    ImGui.Text("Licence: Mozilla Public License 2.0");
                    ImGui.Text("GitHub: Samsung-Loki/Hreidmar");
                    ImGui.End();
                }
            }

            if (_session != null && ImGui.Begin("File Flashing", ImGuiWindowFlags.AlwaysAutoResize)) {
                if (_pit == null) {
                    if (ImGui.Button("Load PIT by dumping")) {
                        new Thread(() => { 
                            try {
                                _pit = PitData.FromBytes(_session.DumpPit((i, i1) => {
                                    _value = i1 / 100 * i;
                                }));
                                AvailablePartitions = _pit.Entries;
                            } catch (Exception e) { _loggingData += $"Exception occured: {e.Message}!"; }
                        }).Start();
                    }

                    if (ImGui.Button("Load PIT from file")) _showLoadPitSelect = true;
                } else {
                    ImGui.Combo("Partition", ref _currentPitEntry,
                        AvailablePartitions.Select(x => x.PartitionName).ToArray(), AvailablePartitions.Count);
                    ImGui.SameLine();
                    if (ImGui.Button("Add")) {
                        Partitions.Add(_selectedFile, AvailablePartitions[_currentPitEntry]);
                        AvailablePartitions.RemoveAt(_currentPitEntry);
                        _currentPitEntry = 0;
                        _selectedFile = "";
                    }
                    ImGui.InputText("File", ref _selectedFile, 256);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse")) _showFileFlashSelect = true;
                    ImGui.Combo("Partition to delete", ref _partitionToDelete,
                        Partitions.Select(x => x.Value.PartitionName).ToArray(), Partitions.Count);
                    ImGui.SameLine();
                    if (ImGui.Button("Delete")) {
                        AvailablePartitions.Add(Partitions.ElementAt(_partitionToDelete).Value);
                        Partitions.Remove(Partitions.ElementAt(_partitionToDelete).Key);
                    }
                    ImGui.Separator();
                    ImGui.Text("Partitions to flash:");
                    foreach (KeyValuePair<string, PitEntry> entry in Partitions) 
                        ImGui.Text($"{entry.Value.PartitionName}: {Path.GetFileName(entry.Key)}");
                    ImGui.Separator();
                    ImGui.ProgressBar(_flashValue);
                    if (ImGui.Button("Flash")) {
                        new Thread(() => {
                            try {
                                var length = new List<ulong>();
                                _flashMax = 0;
                                foreach (KeyValuePair<string, PitEntry> entry in Partitions) {
                                    if (entry.Key.EndsWith(".lz4")) {
                                        using var file = new FileStream(entry.Key, FileMode.Open, FileAccess.Read);
                                        using var stream = LZ4Stream.Decode(file);
                                        _flashMax += stream.Length;
                                        length.Add((ulong)stream.Length);
                                    } else {
                                        _flashMax += (ulong)new FileInfo(entry.Key).Length;
                                        length.Add((ulong)new FileInfo(entry.Key).Length);
                                    }
                                }
                                _session.ReportTotalBytes(length);
                                foreach (KeyValuePair<string, PitEntry> entry in Partitions) {
                                    using var file = new FileStream(entry.Key, FileMode.Open, FileAccess.Read);
                                    file.Seek(0, SeekOrigin.Begin);
                                    if (entry.Key.EndsWith(".lz4")) {
                                        using var stream = LZ4Stream.Decode(file);
                                        _session.FlashFile(stream, entry.Value, (i) => {
                                            _flashValue = _flashMax / 100 * i;
                                        });
                                    } else {
                                        _session.FlashFile(file, entry.Value, (i) => {
                                            _flashValue = _flashMax / 100 * i;
                                        });
                                    }
                                }
                            } catch (Exception e) { _loggingData += $"Flashing failed: {e.Message}!"; }
                        }).Start();
                    }
                    if (ImGui.Button("Unload PIT"))
                        _pit = null;
                }
            }
            /*
            if (_session != null && ImGui.Begin("Device Info", ImGuiWindowFlags.AlwaysAutoResize)) {
                var model = _session.Information.ContainsKey("MODEL") ? _session.Information["MODEL"] : "Empty";
                var salesCode = _session.Information.ContainsKey("SALES") ? _session.Information["SALES"] : "Empty";
                var firmware = _session.Information.ContainsKey("VER") ? _session.Information["VER"] : "Empty";
                var did = _session.Information.ContainsKey("DID") ? _session.Information["DID"] : "Empty";
                ImGui.Text($"Model: {model}");
                ImGui.Text($"Region: {salesCode}");
                ImGui.Text($"Firmware: {firmware}");
                ImGui.Text($"DID: {did}");
                ImGui.End();
            }
            */
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
                    if (CurrentDevice.Vid != DeviceSession.SamsungKVid || !DeviceSession
                        .SamsungPids.ToList().Contains(CurrentDevice.Pid)) 
                        ImGui.Text("This device is not a Hreidmar-compatible download mode Samsung device!");
                } catch { /* Ignore */ }
                if (ImGui.Button("Connect")) {
                    try { _session = new DeviceSession(CurrentDevice, _options, (str) => { _loggingData += str + "\n"; } ); }
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
                ImGui.ProgressBar(_value);
                if (ImGui.Button("Dump to \"dump.pit\"")) {
                    new Thread(() => { 
                        try {
                            File.WriteAllBytes("dump.pit", _session.DumpPit((i, i1) => {
                                _value = i1 / 100 * i;
                            }));
                        } catch (Exception e) { _loggingData += $"Dumping failed: {e.Message}!"; }
                    }).Start();
                }
                if (_unlockPit) {
                    ImGui.InputText("PIT", ref _pitFileToFlash, 256);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse")) _showLoadPitSelect = true;
                    if (ImGui.Button("Repartition")) {
                        new Thread(() => {
                            try { _session.FlashPit(File.ReadAllBytes(_pitFileToFlash)); } 
                            catch (Exception e) { _loggingData += $"Repartition failed: {e.Message}!"; }
                        }).Start();
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
                    }
                }
            }
            _imGuiRenderer.AfterLayout();
            base.Draw(gameTime);
        }
    }
}
