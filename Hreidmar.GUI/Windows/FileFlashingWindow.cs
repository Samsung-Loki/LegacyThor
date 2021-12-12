using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using Hreidmar.Enigma;
using Hreidmar.Enigma.Packets;
using Hreidmar.Enigma.PIT;
using ImGuiNET;
using K4os.Compression.LZ4.Streams;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Hreidmar.GUI.Windows
{
    public class FileFlashingWindow : Window
    {
        /// <summary>
        /// Current PIT entry index
        /// </summary>
        private int _currentPitEntry;
        
        /// <summary>
        /// Files to be flashed onto partitions
        /// </summary>
        private readonly Dictionary<string, PitEntry> _partitions = new();
        
        /// <summary>
        /// Available PIT partitions
        /// </summary>
        private List<PitEntry> _availablePartitions;
        
        /// <summary>
        /// _partitions index for deletion
        /// </summary>
        private int _partitionToDelete;
        
        /// <summary>
        /// Progress bar value
        /// </summary>
        private float _flashValue;
        
        /// <summary>
        /// Progress bar max value
        /// </summary>
        private float _flashMax;
        
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
            if (ImGui.Begin("File Flashing", ImGuiWindowFlags.AlwaysAutoResize)) {
                var session = WindowsManager.GetWindow<DevicesWindow>("devices").Session;
                if (WindowsManager.GetWindow<PitLoadSelectWindow>("loadselect").PitData == null) {
                    if (ImGui.Button("Load PIT by dumping")) {
                        new Thread(() => { 
                            try { 
                                WindowsManager.GetWindow<PitLoadSelectWindow>("loadselect").PitData = 
                                    PitData.FromBytes(session.DumpPit((i, i1) => { 
                                        _flashValue = i1 / 100 * i; 
                                    }));
                                _availablePartitions = WindowsManager.GetWindow<PitLoadSelectWindow>("loadselect").PitData.Entries;
                            } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Exception occured: {e}"); }
                        }).Start();
                    }

                    if (ImGui.Button("Load PIT from file")) WindowsManager.Open("loadselect");
                } else { 
                    ImGui.Combo("Partition", ref _currentPitEntry,
                       _availablePartitions.Select(x => x.PartitionName).ToArray(), _availablePartitions.Count); 
                    ImGui.SameLine();
                    if (ImGui.Button("Add")) {
                       _partitions.Add(WindowsManager.GetWindow<FileFlashSelectWindow>("fileselect").SelectedFile, _availablePartitions[_currentPitEntry]);
                       _availablePartitions.RemoveAt(_currentPitEntry);
                       _currentPitEntry = 0;
                       WindowsManager.GetWindow<FileFlashSelectWindow>("fileselect").SelectedFile = "";
                    }
                    ImGui.InputText("File", ref WindowsManager.GetWindow<FileFlashSelectWindow>("fileselect").SelectedFile, 256);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse")) WindowsManager.Open("fileselect");
                    ImGui.Combo("Partition to delete", ref _partitionToDelete,
                        _partitions.Select(x => x.Value.PartitionName).ToArray(), _partitions.Count);
                    ImGui.SameLine();
                    if (ImGui.Button("Delete")) {
                        _availablePartitions.Add(_partitions.ElementAt(_partitionToDelete).Value);
                        _partitions.Remove(_partitions.ElementAt(_partitionToDelete).Key);
                    }
                    ImGui.Separator();
                    ImGui.Text("Partitions to flash:");
                    foreach (KeyValuePair<string, PitEntry> entry in _partitions) 
                        ImGui.Text($"{entry.Value.PartitionName}: {Path.GetFileName(entry.Key)}");
                    ImGui.Separator();
                    ImGui.ProgressBar(_flashValue);
                    if (ImGui.Button("Flash")) {
                        new Thread(() => {
                            try {
                                var length = new List<ulong>();
                                _flashMax = 0;
                                foreach (KeyValuePair<string, PitEntry> entry in _partitions) {
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
                                session.ReportTotalBytes(length);
                                foreach (KeyValuePair<string, PitEntry> entry in _partitions) {
                                    using var file = new FileStream(entry.Key, FileMode.Open, FileAccess.Read);
                                    file.Seek(0, SeekOrigin.Begin);
                                    if (entry.Key.EndsWith(".lz4")) {
                                        using var stream = LZ4Stream.Decode(file);
                                        session.FlashFile(stream, entry.Value, i => {
                                            _flashValue = _flashMax / 100 * i;
                                        });
                                    } else {
                                        session.FlashFile(file, entry.Value, i => {
                                            _flashValue = _flashMax / 100 * i;
                                        });
                                    }
                                }
                            } catch (Exception e) { WindowsManager.GetWindow<LogsWindow>("logs").Log($"Flashing failed: {e}"); }
                        }).Start();
                    }
                    if (ImGui.Button("Unload PIT"))
                        WindowsManager.GetWindow<PitLoadSelectWindow>("loadselect").PitData = null;
                }
                ImGui.End();
            }
        }
    }
}