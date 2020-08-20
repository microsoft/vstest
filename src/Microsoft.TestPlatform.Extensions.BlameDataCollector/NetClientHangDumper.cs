// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Diagnostics.NETCore.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    internal class NetClientHangDumper : IHangDumper
    {
        public void Dump(int processId, string outputDirectory, DumpTypeOption type)
        {
            var process = Process.GetProcessById(processId);
            var processTree = process.GetProcessTree();

            if (EqtTrace.IsVerboseEnabled)
            {
                if (processTree.Count > 1)
                {
                    EqtTrace.Verbose("NetClientHangDumper.Dump: Dumping this process tree (from bottom):");
                    ConsoleOutput.Instance.Information(false, "Blame: Dumping this process tree (from bottom):");

                    foreach (var p in processTree.OrderBy(t => t.Level))
                    {
                        EqtTrace.Verbose($"NetClientHangDumper.Dump: {(p.Level != 0 ? " + " : " > ")}{new string('-', p.Level)} {p.Process.Id} - {p.Process.ProcessName}");
                        ConsoleOutput.Instance.Information(false, $"Blame: {(p.Level != 0 ? " + " : " > ")}{new string('-', p.Level)} {p.Process.Id} - {p.Process.ProcessName}");
                    }
                }
                else
                {
                    EqtTrace.Verbose($"NetClientHangDumper.Dump: Dumping {process.Id} - {process.ProcessName}.");
                    ConsoleOutput.Instance.Information(false, $"Blame: Dumping {process.Id} - {process.ProcessName}");
                }
            }

            var bottomUpTree = processTree.OrderByDescending(t => t.Level).Select(t => t.Process);

            foreach (var p in bottomUpTree)
            {
                try
                {
                   p.Suspend();
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"NetClientHangDumper.Dump: Error suspending process {p.Id} - {p.ProcessName}: {ex}.");
                }
            }

            foreach (var p in bottomUpTree)
            {
                try
                {
                    var outputFile = Path.Combine(outputDirectory, $"{p.ProcessName}_{p.Id}_{DateTime.Now:yyyyMMddTHHmmss}_hangdump.dmp");
                    EqtTrace.Verbose($"NetClientHangDumper.CollectDump: Selected dump type {type}. Dumping {process.Id} - {process.ProcessName} in {outputFile}. ");

                    var client = new DiagnosticsClient(p.Id);

                    // Connecting the dump generation logging to verbose output to avoid changing the interfaces again -> EqtTrace.IsVerboseEnabled
                    // before we test this on some big repo.
                    client.WriteDump(type == DumpTypeOption.Full ? DumpType.Full : DumpType.Normal, outputFile, logDumpGeneration: false);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"NetClientHangDumper.Dump: Error dumping process {p.Id} - {p.ProcessName}: {ex}.");
                }
            }

            foreach (var p in bottomUpTree)
            {
                try
                {
                    EqtTrace.Verbose($"NetClientHangDumper.Dump: Killing process {p.Id} - {p.ProcessName}.");
                    p.Kill();
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"NetClientHangDumper.Dump: Error killing process {p.Id} - {p.ProcessName}: {ex}.");
                }
            }
        }
    }
}
