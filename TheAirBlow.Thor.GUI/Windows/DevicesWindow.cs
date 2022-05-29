// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Serilog;
using TheAirBlow.Thor.Enigma;
using TheAirBlow.Thor.Enigma.Protocols;

namespace TheAirBlow.Thor.GUI.Windows;

public class DevicesWindow : Window
{
    /// <summary>
    /// Last devices count
    /// </summary>
    private int _lastCount = -1;

    /// <summary>
    /// USB devices
    /// </summary>
    private List<UsbRegistry> _devices = new();

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
            if (device.Device == null)
                continue;
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
        
    /// <summary>
    /// Current session
    /// </summary>
    public DeviceSession? Session;

    /// <summary>
    /// Odin Device Info
    /// </summary>
    private DeviceInfo _info;

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
    /// Draw the Devices window
    /// </summary>
    public override void Draw()
    {
        if (ImGui.Begin("Device", ImGuiWindowFlags.AlwaysAutoResize)) {
            if (Session == null) {
                ImGui.Combo("Select device", ref _currentDeviceIndex, _devicesNames.ToArray(), _devicesNames.Count);
                ImGui.Checkbox("Show non-compatible devices", ref _showNonSamsung);
                if (CurrentDevice != null) {
                    try {
                        if (CurrentDevice.Vid != DeviceSession.SamsungKVid 
                            || !DeviceSession.SamsungPids.Contains(CurrentDevice.Pid)) 
                            ImGui.Text("This device is not a Hreidmar-compatible download mode Samsung device!");
                    } catch { /* Ignore */ }
                
                    if (ImGui.Button("Connect")) {
                        Program.Logger.Information($"Connecting to {CurrentDevice.Name} " +
                                                   $"(0x{CurrentDevice.Vid:X4}/0x{CurrentDevice.Pid:X4})...");
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
            }
            ImGui.End();
        }
    }
}