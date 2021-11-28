using System;
using System.IO;
using Hreidmar.Library;
using Hreidmar.Library.PIT;
using Spectre.Console;

namespace Hreidmar.Application
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            AnsiConsole.MarkupLine("[green]Welcome to Hreidmar shell![/]");
            DeviceSession session = null;
            while (true) {
                string cmd = AnsiConsole.Ask<string>("[yellow]>[/] ");
                string[] cmds = cmd.Split(' ');
                if (cmds.Length == 0)
                    AnsiConsole.MarkupLine($"[red]Command name required![/]");
                try {
                    switch (cmds[0]) {
                        case "init":
                            session = new DeviceSession();
                            AnsiConsole.MarkupLine($"[green]Success![/]");
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
                }
            }
        }
    }
}