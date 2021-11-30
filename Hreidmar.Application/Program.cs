using System;
using System.IO;
using System.Linq;
using Hreidmar.Library;
using Hreidmar.Library.PIT;
using LibUsbDotNet;
using LibUsbDotNet.LudnMonoLibUsb;
using LibUsbDotNet.Main;
using MonoLibUsb;
using Spectre.Console;

namespace Hreidmar.Application
{
    public static class Program
    {
        public static void Main(string[] args) {
            AnsiConsole.MarkupLine("[green]Welcome to Hreidmar shell![/]");
            AnsiConsole.MarkupLine("[yellow]Options will only apply before initialization![/]");
            DeviceSession session = null;
            Exception lastException = null;
            var options = new DeviceSession.Options();
            var exceptionsCount = 0;
            while (true) {
                string cmd = AnsiConsole.Ask<string>("[yellow]>[/] ");
                string[] cmds = cmd.Split(' ');
                if (cmds.Length == 0)
                    AnsiConsole.MarkupLine($"[red]Command name required![/]");
                try {
                    switch (cmds[0]) {
                        case "exception":
                            if (lastException == null) {
                                switch (exceptionsCount) {
                                    case 20:
                                        AnsiConsole.MarkupLine("[red]You wanted it? HERE YOU HAVE IT![/]");
                                        throw new Exception("You fool.");
                                    case 15:
                                        AnsiConsole.MarkupLine("[red]Just stop. Please.[/]");
                                        break;
                                    case 10:
                                        AnsiConsole.MarkupLine("[red]NO EXCEPTIONS OCCURED YET!!1![/]");
                                        break;
                                    case 5:
                                        AnsiConsole.MarkupLine("[red]What do you want from me?[/]");
                                        break;
                                    default:
                                        AnsiConsole.MarkupLine("[red]No exceptions occured yet![/]");
                                        break;
                                }

                                exceptionsCount++;
                            }
                            else AnsiConsole.WriteException(lastException);
                            break;
                        case "reboot":
                            AnsiConsole.MarkupLine(options.Reboot 
                                ? "[green]Reboot option disabled![/]"
                                : "[green]Reboot option enabled![/]");
                            options.Reboot = !options.Reboot;
                            break;
                        case "resume":
                            AnsiConsole.MarkupLine(options.Resume 
                                ? "[green]Resume option disabled![/]"
                                : "[green]Resume option enabled![/]");
                            options.Resume = !options.Resume;
                            break;
                        case "init":
                            if (cmds.Length > 1 && int.Parse(cmds[1]) >= UsbDevice.AllLibUsbDevices.Count) {
                                AnsiConsole.MarkupLine("[red]Invalid device ID![/]");
                                break;
                            }
                            session = cmds.Length > 1 
                                ? new DeviceSession((MonoUsbDevice)UsbDevice.AllLibUsbDevices[int.Parse(cmds[1])].Device, options) 
                                : new DeviceSession(options);
                            AnsiConsole.MarkupLine($"[green]Success![/]");
                            break;
                        case "lsusb":
                            var table1 = new Table();
                            table1.AddColumn("ID");
                            table1.AddColumn("VID");
                            table1.AddColumn("PID");
                            table1.AddColumn("Manufacturer");
                            table1.AddColumn("Product Name");
                            table1.AddColumn("Serial Code");
                            table1.AddColumn("Samsung");
                            for (var i = 0; i < UsbDevice.AllLibUsbDevices.Count; i++) {
                                var device = (MonoUsbDevice)UsbDevice.AllLibUsbDevices[i].Device;
                                if (UsbDevice.LastErrorString.Contains("Access denied", StringComparison.CurrentCultureIgnoreCase))
                                    throw new Exception("Access denied!");
                                var samsung = device.Info.Descriptor.VendorID == DeviceSession.SamsungKVid 
                                              && DeviceSession.SamsungPids.ToList().Contains(device.Info.Descriptor.ProductID);
                                table1.AddRow(i.ToString(), $"{device.Info.Descriptor.VendorID:X4}", 
                                    $"{device.Info.Descriptor.ProductID:X4}", device.Info.ManufacturerString, 
                                    device.Info.ProductString, device.Info.SerialString, samsung.ToString());
                            }
                            AnsiConsole.Write(table1);
                            break;
                        case "dispose":
                            session?.Dispose();
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
                                            entry.BlockCount.ToString(),entry.PartitionName, entry.FlashName, entry.FotaName);
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
                            AnsiConsole.MarkupLine($"[bold]exception[/] - Prints last exception info");
                            AnsiConsole.MarkupLine($"[bold]dispose[/] - Closes current connection");
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
                    AnsiConsole.MarkupLine($"[red]Exception occured: {e.Message}[/]");
                    File.WriteAllText("stacktrace.log", e.ToString());
                    lastException = e;
                }
            }
        }
    }
}