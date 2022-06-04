// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.Tar;
using ImGuiNET;
using K4os.Compression.LZ4.Streams;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Serilog;
using TheAirBlow.Thor.Enigma;
using TheAirBlow.Thor.Enigma.Exceptions;
using TheAirBlow.Thor.Enigma.PIT;
using TheAirBlow.Thor.Enigma.Protocols;
using TheAirBlow.Thor.Enigma.Protocols.Odin;
using TheAirBlow.Thor.Enigma.Receivers;
using TheAirBlow.Thor.Enigma.Senders;

namespace TheAirBlow.Thor.GUI.Windows;

public class DevicesWindow : Window
{
    #region USB Devices
    /// <summary>
    /// Last devices count
    /// </summary>
    private int _lastCount = -1;

    /// <summary>
    /// USB devices
    /// </summary>
    private List<UsbRegistry> _devices = new();

    /// <summary>
    /// Did we warn about running not as admin?
    /// </summary>
    private bool _warned;

    /// <summary>
    /// Refresh devices
    /// </summary>
    public void Refresh()
    {
        if (_lastCount == UsbDevice.AllDevices.Count
            && _showNonSamsung == _lastShowNonSamsung) return;
        var tmp = new List<string>();
        _devices.Clear();
        foreach (UsbRegistry device in UsbDevice.AllDevices) {
            if (device.Device == null) {
                if (!_warned) {
                    WindowsManager.ShowPopup("Unable to access USB",
                        "Please run Thor as admin if you want to connect to a device!");
                    _warned = true;
                }
                continue;
            }
            if (!_showNonSamsung)
                if (device.Vid != DeviceSession.SamsungKVid 
                    || !DeviceSession.SamsungPids.Contains(device.Pid))
                    continue;
            _devices.Add(device);
            var str = string.IsNullOrEmpty(device.FullName)
                ? $"Unnamed device (0x{device.Vid:X4}/0x{device.Pid:X4})"
                : device.FullName;
            tmp.Add(str);
        }

        _devicesNames = tmp;
        _lastCount = UsbDevice.AllDevices.Count;
        _lastShowNonSamsung = _showNonSamsung;
    }

    /// <summary>
    /// Current device index
    /// </summary>
    private int _currentDeviceIndex;
        
    /// <summary>
    /// Device names
    /// </summary>
    private List<string> _devicesNames = new();

    /// <summary>
    /// Selected USB device
    /// </summary>
    public UsbRegistry? CurrentDevice {
        get {
            if (_devices.Count <= _currentDeviceIndex) return null;
            return _devices[_currentDeviceIndex];
        }
    }
    
    /// <summary>
    /// Show non-samsung devices
    /// </summary>
    private bool _lastShowNonSamsung;

    /// <summary>
    /// Show non-samsung devices
    /// </summary>
    private bool _showNonSamsung;
    #endregion
    #region Session
    /// <summary>
    /// Current session
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public DeviceSession? Session;

    /// <summary>
    /// Odin Device Info
    /// </summary>
    private DeviceInfo _info = null!;
    #endregion
    #region Flash Tool
    /// <summary>
    /// Original PIT content
    /// </summary>
    private byte[] _pitOriginal;
    
    /// <summary>
    /// PIT file used for flashing
    /// </summary>
    private PitFile? _pit;

    /// <summary>
    /// Flash mode selected
    /// </summary>
    private int _flashMode;

    /// <summary>
    /// T-Flash (SD-card flash)
    /// </summary>
    private bool _tFlash;
    
    /// <summary>
    /// Repartition the device
    /// </summary>
    private bool _repartition;
    
    /// <summary>
    /// Reset flash counter
    /// </summary>
    private bool _resetCounter = true;

    /// <summary>
    /// Flash modes
    /// </summary>
    private readonly string[] _flashModes = {
        "BL/AP/CP/CSC", "Manual", 
        "Latest Firmware (Auto)"
    };

    /// <summary>
    /// Not very reliable, but it works
    /// </summary>
    private string[] _inputBoxes = new string[7] {
        "BL", "AP", "CP", "CSC", "PIT", "UNUSED", "AUTO-SELECT"
    };

    /// <summary>
    /// PIT partitions to show
    /// </summary>
    private string[] _pitPartitions;

    /// <summary>
    /// Paths to files in PIT
    /// </summary>
    private string[] _paths;

    /// <summary>
    /// PIT partition selected
    /// </summary>
    private int _partitionSelected;

    /// <summary>
    /// Are we currently flashing?
    /// </summary>
    private bool _isFlashing;
    
    /// <summary>
    /// Allow flashing
    /// </summary>
    private bool _allowFlash;

    /// <summary>
    /// Allow cancelling
    /// </summary>
    private bool _allowCancel;

    /// <summary>
    /// Cancel flashing operation
    /// </summary>
    private bool _cancelFlash;

    /// <summary>
    /// Current progress bar fraction
    /// </summary>
    private float _currentFraction;
    
    /// <summary>
    /// Total progress bar fraction
    /// </summary>
    private float _totalFraction;

    /// <summary>
    /// Flashing status
    /// </summary>
    private string _flashStatus = "";
    #endregion

    /// <summary>
    /// Reset all variables
    /// </summary>
    private void Reset()
    {
        _pit = null;
        _flashMode = 0;
        _tFlash = false;
        _repartition = false;
        _resetCounter = true;
        _pitPartitions = null!;
        _paths = null!;
        _inputBoxes = new string[7] {
            "", "", "", "", "", "", ""
        };
        _partitionSelected = 0;
        _isFlashing = false;
    }

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
    /// Visible name of the window
    /// </summary>
    public override string VisibleName => "Devices & Flash Tool";
    
    /// <summary>
    /// Enable debug features
    /// </summary>
    private bool _debug = false;

    /// <summary>
    /// Draw the Devices/FlashTool window
    /// </summary>
    public override void Draw()
    {
        FlashTool();
        Devices();
    }

    private void Devices()
    {
        if (ImGui.Begin("Device", ImGuiWindowFlags.AlwaysAutoResize)) {
            if (Session == null) {
                if (_debug && ImGui.Button("Fake connection (debug)")) {
                    _info = new DeviceInfo();
                    _info.Model = "SM-A207F";
                    _info.Region = "SER";
                    _info.CarrierID = "69420";
                    _info.SerialCode = "SEF78EF6FE46S";
                    Session = new DeviceSession();
                    Session.ProtocolType = DeviceSession.ProtocolTypeEnum.Odin;
                    Reset();
                }
                
                ImGui.Text("Select device");
                ImGui.SameLine();
                ImGui.PushItemWidth(400);
                ImGui.Combo("", ref _currentDeviceIndex, _devicesNames.ToArray(), _devicesNames.Count);
                try {
                    if (CurrentDevice != null && (CurrentDevice.Vid != DeviceSession.SamsungKVid 
                                                  || !DeviceSession.SamsungPids.Contains(CurrentDevice.Pid)))
                        ImGui.Text("This device is not a Samsung device in download mode!");
                } catch { /* Ignore */ }
                ImGui.Checkbox("Show non-compatible devices", ref _showNonSamsung);
                if (CurrentDevice != null) {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 80);
                    if (ImGui.Button("Connect")) {
                        Program.Logger.Information($"Connecting to {CurrentDevice.Name} " +
                                                   $"(0x{CurrentDevice.Vid:X4}/0x{CurrentDevice.Pid:X4})...");
                        Reset();
                        try {
                            Session = new DeviceSession(CurrentDevice, Program.Logger);
                        } catch (Exception e) {
                            Session = null;
                            Program.Logger.Error(e, "An exception occured!");
                            Program.Logger.Error($"Last error: {UsbDevice.LastErrorNumber} {UsbDevice.LastErrorString}");
                            WindowsManager.ShowPopup("Unable to connect", e.Message);
                        }

                        if (Session?.ProtocolType == DeviceSession.ProtocolTypeEnum.Odin)
                            _info = ((OdinProtocol) Session?.Protocol!).GetDeviceInfo();
                    }
                }
            } else {
                ImGui.BeginDisabled(_isFlashing);
                if (ImGui.Button("Disconnect")) {
                    Program.Logger.Information($"Disconnected from {CurrentDevice?.Name} " +
                                               $"(0x{CurrentDevice?.Vid:X4}/0x{CurrentDevice?.Pid:X4})!");
                    Session.Dispose(); Session = null;
                }
                ImGui.SameLine();
                ImGui.Text($"Protocol: {Session?.ProtocolType.ToString()}");
                if (Session?.ProtocolType == DeviceSession.ProtocolTypeEnum.Odin) {
                    ImGui.Separator();
                    ImGui.Text($"Model: {_info.Model} | Region: {_info.Region}");
                    ImGui.Text($"Serial Code: {_info.SerialCode}");
                    ImGui.Text($"Carrier ID: {_info.CarrierID}");
                }
                ImGui.Separator();
                if (ImGui.Button("Dump PIT")) {
                    var path = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop), $"{_info.Model}.pit");
                    if (File.Exists(path)) {
                        WindowsManager.ShowPopup("PIT dump cancelled",
                            $"{path} already exists!");
                        return;
                    }
                    using (var stream = new FileStream(path, FileMode.Create))
                        ((OdinProtocol) Session?.Protocol!).DumpPit(stream);
                    WindowsManager.ShowPopup("PIT successfully dumped",
                        $"File was saved as {path}");
                }
                ImGui.SameLine();
                if (ImGui.Button("Reboot")) {
                    WindowsManager.ShowPopup("Thor GUI",
                        $"Device is rebooting!");
                    ((OdinProtocol) Session?.Protocol!).Reboot();
                    Session = null;
                }
                ImGui.SameLine();
                if (ImGui.Button("Shutdown")) {
                    WindowsManager.ShowPopup("Thor GUI",
                        $"Device is shutting down!");
                    ((OdinProtocol) Session?.Protocol!).Shutdown();
                    Session = null;
                }
                ImGui.SameLine();
                if (ImGui.Button("Reboot into Odin")) {
                    WindowsManager.ShowPopup("Thor GUI",
                        $"Device is rebooting into Download mode!");
                    ((OdinProtocol) Session?.Protocol!).OdinReboot();
                    Session = null;
                }
                ImGui.SameLine();
                if (ImGui.Button("Factory Reset")) {
                    WindowsManager.ShowPopup("Thor GUI",
                        $"Successfully erased UserData!");
                    ((OdinProtocol) Session?.Protocol!).NandEraseAll();
                    Session = null;
                }
                ImGui.EndDisabled();
            }
            ImGui.End();
        }
    }

    private void FlashTool()
    {
        if ((CurrentDevice == null || !CurrentDevice!.IsAlive)
            && Session != null && !_debug) {
            Session = null; WindowsManager.ShowPopup("Device unexpectedly disconnected",
                _isFlashing ? "Do not panic, it is not over yet!\n" +
                              "Connect your device back again, and try to flash again.\n" +
                              "Do not restart or power off the device, or else..." : 
                    "You're fine - just reconnect the device.\n" +
                    "If you didn't touch the cable, please check\n" +
                    "if it's in a good state, and replace it if needed.");
        }

        if (Session is {ProtocolType: DeviceSession.ProtocolTypeEnum.Odin}
            && ImGui.Begin("Flash Tool", ImGuiWindowFlags.AlwaysAutoResize)) {
            if (!_isFlashing) {
                ImGui.Text("Mode");
                ImGui.SameLine();
                ImGui.Combo("", ref _flashMode, _flashModes, 3);
                ImGui.Separator();
                ImGui.Checkbox("T-Flash", ref _tFlash);
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Flashes inserted SD-Card instead " +
                                     "of the device itself.");
                ImGui.SameLine();
                ImGui.Checkbox("Re-Partition", ref _repartition);
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Wipes ALL partitions (including bootloader) " +
                                     "and repartitions the device. Very dangerous!");
                ImGui.SameLine();
                ImGui.Checkbox("Reset Flash Counter", ref _resetCounter);
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Resets the flash counter, practically " +
                                     "useless but recommended to be kept ticked.");
                ImGui.Separator();
                if (ImGui.BeginTable("PIT", 3)) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("PIT");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(655);
                    ImGui.InputText($"##pit", ref _inputBoxes[4], 25565);
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Browse##pit"))
                        WindowsManager.OpenFilePicker($"Select .pit file", "pit",
                            ".pit");
                    ImGui.SameLine();
                    if (ImGui.Button($"Dump")) {
                        using var stream = new MemoryStream();
                        ((OdinProtocol) Session?.Protocol!).DumpPit(stream);
                        _pitOriginal = stream.ToArray();
                        _pit = new PitFile(stream);
                        _inputBoxes[4] = "<dumped>";

                        var list = new List<string>();
                        foreach (var i in _pit!.Entries) {
                            var p2 = string.IsNullOrEmpty(i.FileName)
                                ? ""
                                : $" (${i.FileName})";
                            list.Add($"{i.Name}{p2}");
                        }

                        _pitPartitions = list.ToArray();
                        _paths = (string[]) _pitPartitions.Clone();
                        for (var i = 0; i < _paths.Length; i++)
                            _paths[i] = "";

                        WindowsManager.ShowPopup("PIT dumped",
                            "PIT file dumped successfully!");
                    }

                    if (WindowsManager.FilePickerID == "pit") {
                        _inputBoxes[4] = WindowsManager.SelectedFilePickerPath!;
                        WindowsManager.ClearPicker();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"Load") && File.Exists(_inputBoxes[4])) {
                        try {
                            _pit = new PitFile(_inputBoxes[4]);
                            _pitOriginal = File.ReadAllBytes(_inputBoxes[4]);

                            var list = new List<string>();
                            foreach (var i in _pit!.Entries) {
                                var p2 = string.IsNullOrEmpty(i.FileName)
                                    ? "" : $" ({i.FileName})";
                                list.Add($"{i.Name}{p2}");
                            }

                            _pitPartitions = list.ToArray();
                            _paths = (string[]) _pitPartitions.Clone();
                            for (var i = 0; i < _paths.Length; i++)
                                _paths[i] = "";

                            WindowsManager.ShowPopup("PIT loaded",
                                "PIT file loaded successfully!");
                        } catch (Exception e) {
                            Program.Logger.Error(e, "An exception occured!");
                            WindowsManager.ShowPopup("Unable to load PIT",
                                "Please select a valid one. Do you " +
                                "want to get your phone bricked?");
                        }
                    }

                    ImGui.EndTable();
                }
                if (_flashMode == 0) ImGui.Text("If you want to use the PIT file from CSC, then don't click anything above");
                ImGui.Separator();
                switch (_flashMode) {
                    case 0:
                        // Input boxes
                        var tars = new[] {
                            "BL", "AP", "CP", "CSC"
                        };

                        if (ImGui.BeginTable("TARS", 3)) {
                            for (var i = 0; i < 4; i++) {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.Text(tars[i]);
                                ImGui.TableNextColumn();
                                ImGui.SetNextItemWidth(770);
                                ImGui.InputText($"##{tars[i]}combo", ref _inputBoxes[i], 25565);
                                ImGui.TableNextColumn();
                                if (ImGui.Button($"Browse##{tars[i]}"))
                                    WindowsManager.OpenFilePicker($"Select {tars[i]} .tar archive", tars[i],
                                        ".md5|.tar");
                            }

                            ImGui.EndTable();
                        }

                        // Make browse actually do it's thing
                        switch (WindowsManager.FilePickerID) {
                            case "BL":
                                _inputBoxes[0] = WindowsManager.SelectedFilePickerPath!;
                                WindowsManager.ClearPicker();
                                break;
                            case "AP":
                                _inputBoxes[1] = WindowsManager.SelectedFilePickerPath!;
                                WindowsManager.ClearPicker();
                                break;
                            case "CP":
                                _inputBoxes[2] = WindowsManager.SelectedFilePickerPath!;
                                WindowsManager.ClearPicker();
                                break;
                            case "CSC":
                                _inputBoxes[3] = WindowsManager.SelectedFilePickerPath!;
                                WindowsManager.ClearPicker();
                                break;
                        }

                        break;
                    case 1:
                        if (_pit == null) {
                            ImGui.Text("PIT file is not yet loaded!");
                            ImGui.Text("Flash button is forcefully disabled.");
                            _allowFlash = false;
                        } else {
                            if (ImGui.BeginTable("PART", 3)) {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.SetNextItemWidth(200);
                                ImGui.Combo("##partcombo", ref _partitionSelected,
                                    _pitPartitions, _pitPartitions.Length);
                                var part = _pit.Entries.FirstOrDefault(x =>
                                    _pitPartitions[_partitionSelected].Contains(x.Name))!;
                                ImGui.TableNextColumn();
                                ImGui.SetNextItemWidth(595);
                                ImGui.InputText($"##part", ref _paths[_partitionSelected], 25565);
                                ImGui.TableNextColumn();
                                if (ImGui.Button($"Browse##part"))
                                    WindowsManager.OpenFilePicker($"Select {part.Name} file", "part");
                                ImGui.EndTable();
                            }

                            if (ImGui.BeginTable("AUTO", 2)) {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.SetNextItemWidth(700);
                                ImGui.InputText($"##auto", ref _inputBoxes[6], 25565);
                                ImGui.TableNextColumn();
                                if (ImGui.Button($"Browse##auto"))
                                    WindowsManager.OpenFilePicker($"Select directory to autoselect from",
                                        "auto", directory: true);
                                ImGui.SameLine();
                                if (ImGui.Button($"Auto-Select")) {
                                    if (!Directory.Exists(_inputBoxes[6])) {
                                        WindowsManager.ShowPopup("Invalid directory",
                                            "Specified directory doesn't exist!");
                                        return;
                                    }

                                    var selected = 0;
                                    foreach (var i in Directory.EnumerateFiles(_inputBoxes[6])) {
                                        var part = _pit.Entries.FirstOrDefault(x
                                            => x.FileName == Path.GetFileName(i));
                                        if (part == null) continue;
                                        var p2 = string.IsNullOrEmpty(part.FileName)
                                            ? ""
                                            : $" ({part.FileName})";
                                        Console.WriteLine($"{part.Name}{p2}");
                                        _paths[Array.IndexOf(_pitPartitions, $"{part.Name}{p2}")] = i;
                                        selected++;
                                    }

                                    WindowsManager.ShowPopup("Auto-Select finished",
                                        $"Successfully auto-selected {selected} partitions!");
                                }

                                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                                    ImGui.SetTooltip("Automatically select files from " +
                                                     "a directory to it's respective partition");
                                ImGui.EndTable();
                            }

                            if (WindowsManager.FilePickerID == "auto") {
                                _inputBoxes[6] = WindowsManager.SelectedFilePickerPath!;
                                WindowsManager.ClearPicker();
                            }
                        }

                        break;
                    case 2:
                        ImGui.Text("Sorry, no plans for this yet :(");
                        // TODO: Syndical support
                        break;
                }
                ImGui.Separator();
                if (ImGui.Button(_allowFlash ? "Disable Flash button" : "Enable Flash button"))
                    _allowFlash = !_allowFlash;
                ImGui.SameLine();
                ImGui.BeginDisabled(!_allowFlash || (_flashMode == 1 && _pit == null));
                if (ImGui.Button("Flash")) {
                    new Thread(FlashingThread).Start();
                    _isFlashing = true;
                }
                
                ImGui.EndDisabled();
            } else {
                ImGui.Text(_flashStatus);

                if (ImGui.BeginTable("PROGRESS", 2)) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Current");
                    ImGui.TableNextColumn();
                    ImGui.ProgressBar(_currentFraction, new Vector2(400, 24));
                
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Total");
                    ImGui.TableNextColumn();
                    ImGui.ProgressBar(_totalFraction, new Vector2(400, 24));
                    ImGui.EndTable();
                }

                ImGui.Separator();
                if (ImGui.Button(_allowCancel ? "Disable Cancel button" : "Enable Cancel button"))
                    _allowCancel = !_allowCancel;
                ImGui.SameLine();
                ImGui.BeginDisabled(!_allowCancel || _cancelFlash);
                if (ImGui.Button("Cancel"))
                    _cancelFlash = true;
                var pit = _repartition
                    ? "\nWARNING: LET THE BOOTLOADER TO FLASH," +
                      "OR ELSE IF YOU REBOOT YOU'LL BE IN EDL!"
                    : "";
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Cancels flashing of next parititions, " +
                                     "but lets the current one finish to prevent " +
                                     "corrupted data and broken session." + pit);
                ImGui.EndDisabled();
                if (_cancelFlash) {
                    ImGui.SameLine();
                    ImGui.Text("Cancelling...");
                }
            }
        }
    }

    /// <summary>
    /// Flashing thread
    /// </summary>
    private void FlashingThread()
    {
        var protocol = (OdinProtocol) Session!.Protocol;
        var total = _repartition ? (long)_pitOriginal.Length : 0;
        var totalDone = 0L;
        
        // Manual mode
        var list = new List<(PitEntry, string)>();
        var sizes = new Dictionary<string, long>();
        Program.Logger.Information("Getting ready to flash...");
        _flashStatus = "Initializing...";

        #region Initialize
        switch (_flashMode) {
            case 0:
                // All of this only to get the PIT file xD
                if (!File.Exists(_inputBoxes[3]) && _pit == null) {
                    WindowsManager.ShowPopup("Unable to get PIT",
                        "CSC archive doesn't exist and no PIT is loaded!");
                    _isFlashing = false;
                    return;
                }
                
                using (var file = new FileStream(_inputBoxes[3],
                    FileMode.Open, FileAccess.Read))
                using (var stream = new TarInputStream(file, Encoding.UTF8)) {
                    var entry = stream.GetNextEntry();
                    while (entry != null) {
                        if (!entry.IsDirectory && entry.Name.EndsWith(".pit")) {
                            try {
                                _pit = new PitFile(stream);
                                total += entry.Size;
                                break;
                            } catch (Exception e) {
                                Program.Logger.Error(e, "An exception occured!");
                                WindowsManager.ShowPopup("Unable to parse PIT",
                                    "Invalid PIT file in CSC!");
                                _isFlashing = false;
                                return;
                            }
                        }
                        entry = stream.GetNextEntry();
                    }
                }
                
                break;
            case 1:
                // Get all of the partitions with file assigned
                foreach (var i in _pitPartitions) {
                    var name = i.Split(" ")[0];
                    var part = _pit!.Entries.FirstOrDefault(
                        x => x.Name == name);
                    list.Add((part, _paths[Array.IndexOf(_pitPartitions, i)])!);
                }
                
                // Filter out non-existent files, get sizes of each file
                var toRemove = new List<(PitEntry, string)>();
                foreach (var (a, i) in list) {
                    if (!File.Exists(i)) {
                        toRemove.Add((a, i));
                        continue;
                    }
                    var size = new FileInfo(i).Length;
                    sizes.Add(i, size);
                    total += (uint)size;
                }

                foreach (var i in toRemove)
                    list.Remove(i);
                break;
            case 2:
                // TODO: Syndical support
                WindowsManager.ShowPopup("Not implemented yet",
                    "Sorry, but Syndical mode is not complete");
                _isFlashing = false;
                return;
        }
        #endregion
        #region Total Size
        protocol.SendTotalBytes(total);
        #endregion
        #region T-Flash
        // Endable T-Flash if needed
        if (_tFlash) protocol.EnableTFlash();
        #endregion
        #region Re-Partition
        // Re-Partition if needed
        if (_repartition) {
            _flashStatus = "Re-partitioning...";
            Program.Logger.Information("Re-Partitioning, woah!");
            protocol.Send(new BasicCmdSender((int)PacketType.PitXmit, 
                    (int)XmitShared.RequestFlash),
                new ByteAck((int)PacketType.PitXmit),
                true);
            protocol.Send(new BasicCmdSender((int)PacketType.PitXmit, 
                    (int)XmitShared.Begin, _pitOriginal.Length),
                new ByteAck((int)PacketType.PitXmit),
                true);
            protocol.Send(new ByteSender(_pitOriginal),
                new ByteAck((int) PacketType.PitXmit),
                true, timeout: protocol.FlashTimeout);
            totalDone += (uint)_pitOriginal.Length;
            _currentFraction = 1;
            _totalFraction = CalcPercentage(totalDone, total);
            protocol.Send(new BasicCmdSender((int)PacketType.PitXmit, 
                    (int)XmitShared.End),
                new ByteAck((int)PacketType.PitXmit),
                true);
        }
        #endregion
        #region Flash stuff
        Program.Logger.Information("Starting to flash!");
        switch (_flashMode) {
            case 0:
                for (var i = 0; i < 4; i++) {
                    if (!File.Exists(_inputBoxes[i]))
                        continue;

                    using var file = new FileStream(_inputBoxes[i],
                        FileMode.Open, FileAccess.Read);
                    using var stream = new TarInputStream(file, Encoding.UTF8);
                    var entry = stream.GetNextEntry();
                    while (entry != null) {
                        if (!entry.IsDirectory) {
                            if (_cancelFlash) {
                                _isFlashing = false;
                                var pit = _repartition 
                                    ? "\nDO NOT RESTART YOUR DEVICE! Flash it again while you can -\n" +
                                      "you wouldn't be to do it if you restart the device!\n" +
                                      "At least flash the bootloader, so you can access Odin mode.\n" +
                                      "Otherwise you would be in EDL, and often Firehose files\n" +
                                      "are available only to offical repair centers for recovery." : "";
                                _repartition = false;
                                var reset = _resetCounter ? 
                                    "\nFlash counter wasn't reset - do not cancel next time." : "";
                                WindowsManager.ShowPopup("Flashing cancelled",
                                    "You cancelled the flashing operation, " +
                                    "but some partitions were still flashed." 
                                    + pit + reset);
                                return;
                            }

                            var pitEntry = _pit!.Entries.FirstOrDefault(x => 
                                x.FileName == entry.Name.Replace(".lz4", ""));
                            if (pitEntry == null) {
                                Program.Logger.Information($"Skipping {entry.Name}, no match found");
                                entry = stream.GetNextEntry();
                                continue;
                            }
                            
                            var ext = string.IsNullOrEmpty(pitEntry.FileName)
                                ? "" : $" ({pitEntry.FileName})";
                            _flashStatus = $"Flashing {pitEntry.Name}{ext}...";
                            Console.WriteLine($"Flashing {pitEntry.Name}{ext} {entry.Name}...");
                            totalDone = Flash(pitEntry, stream, entry.Size, totalDone,
                                total, entry.Name.EndsWith("lz4"));
                        }
                        entry = stream.GetNextEntry();
                    }
                }
                break;
            case 1:
                // Actually flash files
                foreach (var (entry, file) in list) {
                    if (_cancelFlash) {
                        _isFlashing = false;
                        var pit = _repartition 
                            ? "\nDO NOT RESTART YOUR DEVICE! Flash it again while you can -\n" +
                              "you wouldn't be to do it if you restart the device!\n" +
                              "At least flash the bootloader, so you can access Odin mode.\n" +
                              "Otherwise you would be in EDL, and often Firehose files\n" +
                              "are available only to offical repair centers for recovery." : "";
                        _repartition = false;
                        var reset = _resetCounter ? 
                            "\nFlash counter wasn't reset - do not cancel next time." : "";
                        WindowsManager.ShowPopup("Flashing cancelled",
                            "You cancelled the flashing operation, " +
                            "but some partitions were still flashed." 
                            + pit + reset);
                        return;
                    }
                        
                    var ext = string.IsNullOrEmpty(entry.FileName)
                        ? "" : $" ({entry.FileName})";
                    _flashStatus = $"Flashing {entry.Name}{ext}...";
                    Console.WriteLine($"Flashing {entry.Name}{ext}...");
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    totalDone = Flash(entry, stream, sizes[file], totalDone, 
                        total, file.EndsWith("lz4"));
                }
                break;
            case 2:
                break;
        }
        #endregion
        #region Reset Counter
        // Reset flash counter if needed
        if (_resetCounter)
            protocol.Send(new BasicCmdSender((int)PacketType.SessionStart, 
                    (int)SessionStart.ResetFlashCounter),
                new ByteAck((int)PacketType.SessionStart),
                true);
        #endregion
        
        _isFlashing = false;
        WindowsManager.ShowPopup("Flashing finished",
            "Successfully flashed all partitions!");
    }

    /// <summary>
    /// Flash a partition
    /// </summary>
    /// <param name="entry">PIT entry</param>
    /// <param name="stream">Stream to read</param>
    /// <param name="fileSize">File's size</param>
    /// <param name="totalDone">Done in total</param>
    /// <param name="compressed">LZ4 compressed</param>
    /// <param name="totalMax">Maximum total value</param>
    /// <returns>New total progress</returns>
    private long Flash(PitEntry entry, Stream stream, long fileSize, 
        long totalDone, long totalMax, bool compressed = false)
    {
        var transferred = 0L;
        var protocol = (OdinProtocol) Session!.Protocol;
        var sequences = fileSize / protocol.SequenceSize;
        var lastSequence = (int)(fileSize % protocol.SequenceSize);
        if (lastSequence != 0)
            sequences++;

        Stream? original = null;
        if (compressed && !protocol.CompressedSupported) {
            original = stream; // Manual decompression
            stream = LZ4Stream.Decode(original);
        }
        
        var add = compressed && protocol.CompressedSupported ? 4 : 0;
        protocol.Send(new BasicCmdSender((int)PacketType.FileXmit, 
                (int)XmitShared.RequestFlash + add),
            new ByteAck((int)PacketType.FileXmit), 
            true);
        
        for (var i = 0; i < sequences; i++) {
            var last = i == sequences - 1;
            var size = last ? lastSequence : protocol.SequenceSize;
            protocol.Send(new BasicCmdSender((int)PacketType.FileXmit, 
                    (int)XmitShared.Begin + add, size),
                new ByteAck((int)PacketType.FileXmit), 
                true);
            var fileParts = size / protocol.FilePartSize;
            var lastFilePart = size % protocol.FilePartSize;
            if (lastFilePart != 0)
                fileParts++;
            for (var j = 0; j < fileParts; j++) {
                var lbuf = new List<byte>();
                var read = 0;
                
                // Workaround if there's not enough data available
                while (read != protocol.FilePartSize) {
                    var tmpBuf = new byte[protocol.FilePartSize - read];
                    var readLocal = stream.Read(tmpBuf, 0, protocol.FilePartSize - read);
                    read += readLocal; Array.Resize(ref tmpBuf, readLocal);
                    lbuf.AddRange(tmpBuf);
                }

                var buf = lbuf.ToArray();
                var res = (BasicCmdReceiver)protocol.Send(new ByteSender(buf),
                    new BasicCmdReceiver((int)PacketType.FileXmit), 
                    true, timeout: protocol.FlashTimeout);
                if (res.Arguments[0] != j)
                    throw new UnexpectedValueException(
                        $"Bootloader index {res.Arguments[0]} " +
                        $"doesn't match actual index {j}!");
                transferred += protocol.FilePartSize;
                _currentFraction = CalcPercentage(transferred, fileSize);
                _totalFraction = CalcPercentage(totalDone + transferred, totalMax);
            }

            switch (entry.BinaryType) {
                case BinaryType.AP: // Phone
                    protocol.Send(new BasicCmdSender((int)PacketType.FileXmit, 
                            (int)XmitShared.End + add, 0x00,
                            size, 0x00, (int)entry.DeviceType, entry.Identifier,
                            last ? 1 : 0),
                        new ByteAck((int)PacketType.FileXmit), 
                        true);
                    break;
                case BinaryType.CP: // Modem
                    protocol.Send(new BasicCmdSender((int)PacketType.FileXmit, 
                            (int)XmitShared.End + add, 0x01,
                            size, 0x00, (int)entry.DeviceType,
                            last ? 1 : 0),
                        new ByteAck((int)PacketType.FileXmit), 
                        true);
                    break;
            }
        }

        if (original != null) stream.Dispose();
        return totalDone + transferred;
    }

    /// <summary>
    /// Calculate percentage
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="max">Maximum</param>
    /// <returns>0-100 percentage</returns>
    private float CalcPercentage(long value, long max)
        => 100f / max * value;
}