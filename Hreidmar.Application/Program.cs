using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hreidmar.Library;
using Hreidmar.Library.Packets.Inbound;
using Hreidmar.Library.Packets.Playground;
using Hreidmar.Library.PIT;
using K4os.Compression.LZ4.Streams;
using LibUsbDotNet;
using Spectre.Console;
// ReSharper disable AccessToModifiedClosure
// ReSharper disable AccessToDisposedClosure

namespace Hreidmar.Application
{
    public static class Program
    {
        public static void Main(string[] args) {
            AnsiConsole.MarkupLine("[green]Welcome to Hreidmar shell![/]");
            DeviceSession session = null;
            var options = new DeviceSession.OptionsClass();
            var queue = new Dictionary<string, PitEntry>();
            while (true) {
                string cmd = AnsiConsole.Ask<string>("[yellow]>[/] ");
                string[] cmds = cmd.Split(' ');
                if (cmds.Length == 0) {
                    AnsiConsole.MarkupLine($"[red]Command name required![/]");
                    break;
                }
                var stop = new Stopwatch();
                try {
                    switch (cmds[0]) {
                        case "playground":
                            if (session == null) {
                                AnsiConsole.MarkupLine("[red]No device connection was done yet![/]");
                                break;
                            }
                            
                            session.BeginSession();
                            session.SendPacket(new BeginFileDump(), 6000);
                            var packet = (IInboundPacket) new PlaygroundResponse();
                            session.ReadPacket(ref packet, 6000);
                            session.EndSession();
                            break;
                        case "session":
                            if (session == null) {
                                AnsiConsole.MarkupLine("[red]No device connection was done yet![/]");
                                break;
                            }
                                
                            if (cmds.Length < 2) {
                                AnsiConsole.MarkupLine("[red]Sub-command required![/]");
                                break;
                            }
                            
                            switch (cmds[1]) {
                                case "reboot":
                                    stop.Start();
                                    session.Reboot();
                                    stop.Stop();
                                    AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                                    break;
                                case "begin":
                                    stop.Start();
                                    session.BeginSession();
                                    stop.Stop();
                                    AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                                    break;
                                case "end":
                                    stop.Start();
                                    session.EndSession();
                                    stop.Stop();
                                    AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                                    break;
                                case "tflash":
                                    stop.Start();
                                    session.EnableTFlash();
                                    stop.Stop();
                                    AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                                    break;
                                default:
                                    AnsiConsole.MarkupLine("[red]Unknown sub-command![/]");
                                    break;
                            }
                            break;
                        case "reboot":
                            AnsiConsole.MarkupLine(options.Reboot 
                                ? "[green]Reboot option disabled![/]"
                                : "[green]Reboot option enabled![/]");
                            if (session != null) session.Options.Reboot = !session.Options.Reboot;
                            else options.Reboot = !options.Reboot;
                            break;
                        case "resume":
                            AnsiConsole.MarkupLine(options.Resume 
                                ? "[green]Resume option disabled![/]"
                                : "[green]Resume option enabled![/]");
                            if (session != null) session.Options.Resume = !session.Options.Resume;
                            else options.Resume = !options.Resume;
                            break;
                        case "pitdump":
                            if (session == null) {
                                AnsiConsole.MarkupLine("[red]No device connection was done yet![/]");
                                break;
                            }
                            
                            if (cmds.Length < 3) { 
                                AnsiConsole.MarkupLine("[red]Filename required![/]");
                                break;
                            }
                                                                        
                            stop.Start();
                            AnsiConsole.Progress()
                                .Start(ctx => {
                                    using var stream = new FileStream(cmds[2], FileMode.Open, FileAccess.Read);
                                    var task = ctx.AddTask($"[yellow]Dumping PIT[/]");
                                    var pit = session.DumpPit(i => task.Increment(i));
                                    File.WriteAllBytes(cmds[2], pit);
                                    task.Description = "[green]Dumping PIT[/]";
                                    task.StopTask();
                                });
                            AnsiConsole.MarkupLine($"[green]Dump saved as {cmds[2]}![/]");
                            stop.Stop();
                            AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                            break;
                        case "pitflash":
                            if (session == null) {
                                AnsiConsole.MarkupLine("[red]No device connection was done yet![/]");
                                break;
                            }
                            
                            if (cmds.Length < 3) {
                                AnsiConsole.MarkupLine("[red]Filename required![/]");
                                break;
                            }
                                    
                            stop.Start();
                            AnsiConsole.Markup("[yellow]This may render your device unbootable and hardbricked![/]");
                            if (AnsiConsole.Confirm("[yellow]Do you really want to do this?[/]", false))
                                session.FlashPit(File.ReadAllBytes(cmds[2]));
                            stop.Stop();
                            AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                            break;
                        case "flash":
                            if (session == null) {
                                AnsiConsole.MarkupLine("[red]No device connection was done yet![/]");
                                break;
                            }
                            
                            if (cmds.Length < 4) {
                                AnsiConsole.MarkupLine("[red]Filename and partition name required![/]");
                                break;
                            }
                                    
                            stop.Start();
                            if (!session.SessionBegan) session.BeginSession();
                            PitData pit = null;
                            AnsiConsole.Progress()
                                .Start(ctx => {
                                    using var stream = new FileStream(cmds[2], FileMode.Open, FileAccess.Read);
                                    var task = ctx.AddTask($"[yellow]Dumping PIT[/]");
                                    pit = PitData.FromBytes(session.DumpPit(i => task.Increment(i)));
                                    task.Description = "[green]Dumping PIT[/]";
                                    task.StopTask();
                                });
                            var entry2 = pit.Entries.FirstOrDefault(x => x.PartitionName == cmds[3]);
                            if (entry2 == null) {
                                AnsiConsole.MarkupLine("[red]Partition does not exist![/]");
                                break;
                            }
                                    
                            AnsiConsole.MarkupLine($"[green]Identifier:[/] {entry2.Identifier}");
                            AnsiConsole.MarkupLine($"[green]Flash name:[/] {entry2.FlashName}");
                            AnsiConsole.MarkupLine($"[green]Binary Type:[/] {entry2.BinaryType}");
                            if (AnsiConsole.Confirm("[yellow]Do you really want to flash this file?[/]", false)) {
                                AnsiConsole.Progress()
                                    .Start(ctx => {
                                        using var stream = new FileStream(cmds[2], FileMode.Open, FileAccess.Read);
                                        var task = ctx.AddTask($"[yellow]Flashing {entry2.PartitionName}[/]");
                                        if (cmds[2].EndsWith("lz4")) {
                                            AnsiConsole.MarkupLine($"[green]File is compressed using LZ4 algorithm.[/]");
                                            AnsiConsole.MarkupLine($"[green]We'll decompress it and flash it as we get more decompressed data.[/]");
                                            using var decompress = LZ4Stream.Decode(stream);
                                            if (decompress.Length == -1) {
                                                AnsiConsole.MarkupLine("[red]Unable to perform real-time decompression![/]");
                                                AnsiConsole.MarkupLine("[red]Output data size is unknown.[/]");
                                                session.EndSession();
                                                return;
                                            }
                                            session.ReportTotalBytes(new List<ulong> { (ulong)decompress.Length });
                                            session.FlashFile(decompress, entry2, i => task.Increment(i));
                                            task.Description = $"[green]Flashing {entry2.PartitionName}[/]";
                                            task.StopTask();
                                        } else {
                                            session.ReportTotalBytes(new List<ulong> { (ulong)stream.Length });
                                            session.FlashFile(stream, entry2, i => task.Increment(i));
                                            task.Description = $"[green]Flashing {entry2.PartitionName}[/]";
                                            task.StopTask();
                                        }
                                    });
                            }

                            session.EndSession();
                            stop.Stop();
                            AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                            break;
                        case "enqueue":
                            if (cmds.Length < 4) {
                                        AnsiConsole.MarkupLine("[red]Filename and partition name required![/]");
                                        break;
                            }
                                    
                            stop.Start();
                            if (!session.SessionBegan) session.BeginSession();
                            PitData pit1 = null;
                            AnsiConsole.Progress()
                                .Start(ctx => {
                                    using var stream = new FileStream(cmds[2], FileMode.Open, FileAccess.Read);
                                    var task = ctx.AddTask($"[yellow]Dumping PIT[/]");
                                    pit1 = PitData.FromBytes(session.DumpPit(i => task.Increment(i)));
                                    task.Description = "[green]Dumping PIT[/]";
                                    task.StopTask();
                                });
                            var entry3 = pit1.Entries.FirstOrDefault(x => x.PartitionName == cmds[3]);
                            if (entry3 == null) {
                                AnsiConsole.MarkupLine("[red]Partition does not exist![/]");
                                break;
                            }
                                    
                            AnsiConsole.MarkupLine($"[green]Identifier:[/] {entry3.Identifier}");
                            AnsiConsole.MarkupLine($"[green]Flash name:[/] {entry3.FlashName}");
                            AnsiConsole.MarkupLine($"[green]Binary Type:[/] {entry3.BinaryType}");
                            if (AnsiConsole.Confirm("[yellow]Do you really want to enqueue this file to be flashed?[/]", false))
                                queue.Add(cmds[2], entry3);

                            session.EndSession();
                            stop.Stop();
                            AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                            break;
                        case "queueflash":
                            if (session == null) {
                                AnsiConsole.MarkupLine("[red]No device connection was done yet![/]");
                                break;
                            }
                            
                            stop.Start();
                            if (!session.SessionBegan) session.BeginSession();
                            AnsiConsole.Progress()
                                .Start(ctx => {
                                    foreach (KeyValuePair<string, PitEntry> pair in queue) {
                                        var task = ctx.AddTask($"[yellow]Flashing {pair.Value.PartitionName}[/]");
                                        using var stream = new FileStream(pair.Key, FileMode.Open, FileAccess.Read);
                                        if (pair.Key.EndsWith("lz4")) {
                                            AnsiConsole.MarkupLine($"[green]File is compressed using LZ4 algorithm.[/]");
                                            AnsiConsole.MarkupLine($"[green]We'll decompress it and flash it as we get more decompressed data.[/]");
                                            using var decompress = LZ4Stream.Decode(stream);
                                            if (decompress.Length == -1) {
                                                AnsiConsole.MarkupLine("[red]Unable to perform real-time decompression![/]");
                                                AnsiConsole.MarkupLine("[red]Output data size is unknown.[/]");
                                                AnsiConsole.MarkupLine("[red]This file will be skipped.[/]");
                                                continue;
                                            }
                                            session.ReportTotalBytes(new List<ulong> { (ulong)decompress.Length });
                                            session.FlashFile(decompress, pair.Value, i => task.Increment(i));
                                            task.Description = $"[green]Flashing {pair.Value.PartitionName}[/]";
                                            task.StopTask();
                                        } else {
                                            session.ReportTotalBytes(new List<ulong> { (ulong)stream.Length });
                                            session.FlashFile(stream, pair.Value, i => task.Increment(i));
                                            task.Description = $"[green]Flashing {pair.Value.PartitionName}[/]";
                                            task.StopTask();
                                        }
                                        session.FlashFile(stream, pair.Value, i => task.Increment(i));
                                        task.Description = $"[green]Flashing {pair.Value.PartitionName}[/]";
                                        task.StopTask();
                                    }
                                });
                            session.EndSession();
                            stop.Stop();
                            AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                            break;
                        case "clear":
                            queue = new Dictionary<string, PitEntry>();
                            break;
                        case "init":
                            stop.Start();
                            if (cmds.Length > 1 && int.Parse(cmds[1]) >= UsbDevice.AllDevices.Count) {
                                AnsiConsole.MarkupLine("[red]Invalid device ID![/]");
                                break;
                            }
                            session = cmds.Length > 1 
                                ? new DeviceSession(UsbDevice.AllDevices[int.Parse(cmds[1])].Device, options) 
                                : new DeviceSession(options);
                            stop.Stop();
                            AnsiConsole.MarkupLine($"[green]Time elapsed: {stop.Elapsed}[/]");
                            break;
                        case "lsusb":
                            var table1 = new Table();
                            table1.AddColumn("ID");
                            table1.AddColumn("VID");
                            table1.AddColumn("PID");
                            table1.AddColumn("Manufacturer");
                            table1.AddColumn("Product Name");
                            table1.AddColumn("Serial Code");
                            table1.AddColumn("Driver Mode");
                            table1.AddColumn("Samsung");
                            for (var i = 0; i < UsbDevice.AllDevices.Count; i++) {
                                AnsiConsole.WriteLine(UsbDevice.LastErrorString);
                                var device = UsbDevice.AllDevices[i].Device;
                                AnsiConsole.WriteLine(UsbDevice.LastErrorString);
                                if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                                    throw new Exception("Access denied!");
                                var samsung = device.Info.Descriptor.VendorID == DeviceSession.SamsungKVid 
                                              && DeviceSession.SamsungPids.ToList().Contains(device.Info.Descriptor.ProductID);
                                AnsiConsole.WriteLine(UsbDevice.LastErrorString);
                                table1.AddRow(i.ToString(), $"{device.Info.Descriptor.VendorID:X4}", 
                                    $"{device.Info.Descriptor.ProductID:X4}", device.Info.ManufacturerString, 
                                    device.Info.ProductString, device.Info.SerialString, device.DriverMode.ToString(), 
                                    samsung.ToString());
                            }
                            AnsiConsole.Write(table1);
                            break;
                        case "dispose":
                            session?.Dispose();
                            session = null;
                            AnsiConsole.MarkupLine($"[green]Success![/]");
                            break;
                        case "readpit":
                            if (cmds.Length < 3) {
                                AnsiConsole.MarkupLine($"[red]Not enough arguments: PIT filename and sub-command![/]");
                                break;
                            }

                            if (!File.Exists(cmds[1])) {
                                AnsiConsole.MarkupLine($"[red]File does not exist![/]");
                                break;
                            }
                            var data = PitData.FromFile(cmds[1]);
                            switch (cmds[2]) {
                                case "table":
                                    var table = new Table();
                                    table.AddColumn("Identifier");
                                    table.AddColumn("Block Size");
                                    table.AddColumn("Blocks");
                                    table.AddColumn("Partition");
                                    table.AddColumn("Flash Name");
                                    table.AddColumn("FOTA Name");
                                    foreach (var entry in data.Entries)
                                        table.AddRow(entry.Identifier.ToString(), entry.BlockSizeOrOffset.ToString(), 
                                            entry.BlockCount.ToString(), entry.PartitionName, entry.FlashName, entry.FotaName);
                                    AnsiConsole.Write(table);
                                    break;
                                case "file":
                                    if (cmds.Length < 4) {
                                        AnsiConsole.MarkupLine($"[red]Not enough arguments: Output filename![/]");
                                        break;
                                    }

                                    using (var file = new FileStream(cmds[3], FileMode.Create, FileAccess.Write))
                                    using (var stream = new StreamWriter(file)) {
                                        foreach (var entry in data.Entries) {
                                            stream.WriteLine("!===========================!");
                                            stream.WriteLine($"Binary Type: {entry.BinaryType}");
                                            stream.WriteLine($"Device Type: {entry.DeviceType}");
                                            stream.WriteLine($"Identifier: {entry.Identifier}");
                                            var attrs = entry.Attributes == PitEntry.AttributeEnum.Secure
                                                ? "Write"
                                                : "STL";
                                            var attrs2 = entry.UpdateAttributes == PitEntry.AttributeEnum.Secure
                                                ? "Secure"
                                                : "FOTA";
                                            stream.WriteLine($"Attributes: {attrs}");
                                            stream.WriteLine($"Update Attributes: {attrs2}");
                                            stream.WriteLine($"Block Size: {entry.BlockSizeOrOffset}");
                                            stream.WriteLine($"Block Count: {entry.BlockCount}");
                                            stream.WriteLine($"File Offset: {entry.FileOffset}");
                                            stream.WriteLine($"File Size: {entry.FileSize}");
                                            stream.WriteLine($"Partition Name: {entry.PartitionName}");
                                            stream.WriteLine($"File Name: {entry.FlashName}");
                                            stream.WriteLine($"FOTA Name: {entry.FotaName}");
                                            stream.WriteLine();
                                        }
                                    }
                                    
                                    AnsiConsole.MarkupLine($"[green]Success![/]");
                                    break;
                                default:
                                    AnsiConsole.MarkupLine("[red]Unknown sub-command![/]");
                                    break;
                            }
                            
                            break;
                        case "help":
                            AnsiConsole.MarkupLine($"[bold]Information:[/]");
                            AnsiConsole.MarkupLine($"[bold](something)[/] - Optional arguments");
                            AnsiConsole.MarkupLine($"[bold]<something>[/] - Required argument");
                            AnsiConsole.MarkupLine($"[bold]<something, something>[/] - Options (sub-commands)");
                            AnsiConsole.MarkupLine($"\n[bold]Commands:[/]");
                            AnsiConsole.MarkupLine($"[bold]readpit <filename> <table,file<filename>>[/] - Read PIT file");
                            AnsiConsole.MarkupLine($"[bold]session <reboot, start, end>[/] - Session stuff");
                            AnsiConsole.MarkupLine($"[bold]flash <filename, partition>[/] - Flash a file onto partition");
                            AnsiConsole.MarkupLine($"[bold]pitflash <filename>[/] - Flash PIT onto device");
                            AnsiConsole.MarkupLine($"[bold]pitdump <filename>[/] - Dump device's PIT");
                            AnsiConsole.MarkupLine($"[bold]enqueue <filename, partition>[/] - Enqueue file flash onto partition");
                            AnsiConsole.MarkupLine($"[bold]queueflash[/] - Flash files onto paritions in file flash queue");
                            AnsiConsole.MarkupLine($"[bold]clear[/] - Clear file flash queue");
                            AnsiConsole.MarkupLine($"[bold]init (id)[/] - Initialize connection");
                            AnsiConsole.MarkupLine($"[bold]reboot[/] - Switches reboot option");
                            AnsiConsole.MarkupLine($"[bold]resume[/] - Switches resume option");
                            AnsiConsole.MarkupLine($"[bold]lsusb[/] - List all LibUSB devices");
                            AnsiConsole.MarkupLine($"[bold]exit[/] - Leave shell");
                            AnsiConsole.MarkupLine($"[bold]help[/] - Print this");
                            break;
                        case "exit":
                            Environment.Exit(0);
                            break;
                        default:
                            AnsiConsole.MarkupLine($"[red]Unknown command![/]");
                            break;
                    }
                } catch (Exception e) {
                    AnsiConsole.WriteException(e);
                }
            }
        }
    }
}