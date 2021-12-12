using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hreidmar.Enigma;
using ImGuiNET;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Hreidmar.GUI.Windows
{
    public class DevicesWindow : Window
    {
        /// <summary>
        /// Last devices count
        /// </summary>
        private int _lastCount;

        /// <summary>
        /// Refresh devices
        /// </summary>
        public void Refresh()
        {
            if (_lastCount == UsbDevice.AllDevices.Count) return;
            var tmp = new List<string>();
            foreach (UsbRegistry device in UsbDevice.AllDevices) {
                var str = string.IsNullOrEmpty(device.FullName)
                    ? $"Unnamed device (0x{device.Vid:X4}/0x{device.Pid:X4})"
                    : device.FullName;
                tmp.Add(str);
            }

            _devicesNames = tmp;
            _lastCount = UsbDevice.AllDevices.Count;
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
        public UsbRegistry CurrentDevice {
            get {
                if (UsbDevice.AllDevices.Count <= _currentDeviceIndex) return null;
                return UsbDevice.AllDevices[_currentDeviceIndex];
            }
        }
        
        /// <summary>
        /// Current session
        /// </summary>
        public DeviceSession Session;

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
                ImGui.Combo("Select device", ref _currentDeviceIndex, _devicesNames.ToArray(), _devicesNames.Count);
                try {
                    if (CurrentDevice.Vid != DeviceSession.SamsungKVid || !DeviceSession
                        .SamsungPids.ToList().Contains(CurrentDevice.Pid)) 
                        ImGui.Text("This device is not a Hreidmar-compatible download mode Samsung device!");
                } catch { /* Ignore */ }
                if (ImGui.Button("Connect")) {
                    try {
                        Session = new DeviceSession(CurrentDevice, WindowsManager.GetWindow<OptionsWindow>("options").Options,
                            str => { WindowsManager.GetWindow<LogsWindow>("logs").Log(str); });
                    } catch (Exception e) {
                        WindowsManager.GetWindow<LogsWindow>("logs").Log($"Unable to connect: {e}");
                        WindowsManager.GetWindow<LogsWindow>("logs").Log($"Last error: {UsbDevice.LastErrorNumber} {UsbDevice.LastErrorString}");
                    }
                }
                ImGui.End();
            }
        }
    }
}