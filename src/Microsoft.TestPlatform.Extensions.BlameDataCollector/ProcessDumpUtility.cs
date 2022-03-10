// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

internal class ProcessDumpUtility : IProcessDumpUtility
{
    private readonly IProcessHelper _processHelper;
    private readonly IFileHelper _fileHelper;
    private readonly IHangDumperFactory _hangDumperFactory;
    private readonly ICrashDumperFactory _crashDumperFactory;
    private ICrashDumper _crashDumper;
    private string _hangDumpDirectory;
    private string _crashDumpDirectory;
    private bool _wasHangDumped;

    public ProcessDumpUtility()
        : this(new ProcessHelper(), new FileHelper(), new HangDumperFactory(), new CrashDumperFactory())
    {
    }

    public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper, IHangDumperFactory hangDumperFactory, ICrashDumperFactory crashDumperFactory)
    {
        _processHelper = processHelper;
        _fileHelper = fileHelper;
        _hangDumperFactory = hangDumperFactory;
        _crashDumperFactory = crashDumperFactory;
    }

    protected Action<object, string> OutputReceivedCallback => (process, data) =>
        // Log all standard output message of procdump in diag files.
        // Otherwise they end up coming on console in pipleine.
        EqtTrace.Info("ProcessDumpUtility.OutputReceivedCallback: Output received from procdump process: " + data);

    /// <inheritdoc/>
    public IEnumerable<string> GetDumpFiles(bool warnOnNoDumpFiles, bool processCrashed)
    {
        if (!_wasHangDumped)
        {
            _crashDumper.WaitForDumpToFinish();
        }

        // If the process was hang dumped we killed it ourselves, so it crashed when executing tests,
        // but we already have the hang dump, and should not also collect the exit dump that we got
        // from killing the process by the hang dumper.
        IEnumerable<string> crashDumps = _crashDumper?.GetDumpFiles(processCrashed) ?? new List<string>();

        IEnumerable<string> hangDumps = _fileHelper.DirectoryExists(_hangDumpDirectory)
            ? _fileHelper.GetFiles(_hangDumpDirectory, "*_hangdump*.dmp", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();

        var foundDumps = new List<string>();
        foreach (var dumpPath in crashDumps.Concat(hangDumps))
        {
            EqtTrace.Info($"ProcessDumpUtility.GetDumpFiles: Looking for dump file '{dumpPath}'.");
            var found = _fileHelper.Exists(dumpPath);
            if (found)
            {
                EqtTrace.Info($"ProcessDumpUtility.GetDumpFile: Found dump file '{dumpPath}'.");
                foundDumps.Add(dumpPath);
            }
            else
            {
                EqtTrace.Warning($"ProcessDumpUtility.GetDumpFile: Dump file '{dumpPath}' was not found.");
            }
        }

        if (warnOnNoDumpFiles && !foundDumps.Any())
        {
            EqtTrace.Error($"ProcessDumpUtility.GetDumpFile: Could not find any dump file in {_hangDumpDirectory}.");
            throw new FileNotFoundException(Resources.Resources.DumpFileNotGeneratedErrorMessage);
        }

        return foundDumps;
    }

    /// <inheritdoc/>
    public void StartHangBasedProcessDump(int processId, string tempDirectory, bool isFullDump, string targetFramework, Action<string> logWarning = null)
    {
        HangDump(processId, tempDirectory, isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini, targetFramework, logWarning);
    }

    /// <inheritdoc/>
    public void StartTriggerBasedProcessDump(int processId, string testResultsDirectory, bool isFullDump, string targetFramework, bool collectAlways)
    {
        CrashDump(processId, testResultsDirectory, isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini, targetFramework, collectAlways);
    }

    /// <inheritdoc/>
    public void DetachFromTargetProcess(int targetProcessId)
    {
        _crashDumper?.DetachFromTargetProcess(targetProcessId);
    }

    private void CrashDump(int processId, string tempDirectory, DumpTypeOption dumpType, string targetFramework, bool collectAlways)
    {
        var processName = _processHelper.GetProcessName(processId);
        EqtTrace.Info($"ProcessDumpUtility.CrashDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into temporary path '{tempDirectory}'.");
        _crashDumpDirectory = tempDirectory;

        _crashDumper = _crashDumperFactory.Create(targetFramework);
        ConsoleOutput.Instance.Information(false, $"Blame: Attaching crash dump utility to process {processName} ({processId}).");
        _crashDumper.AttachToTargetProcess(processId, tempDirectory, dumpType, collectAlways);
    }

    private void HangDump(int processId, string tempDirectory, DumpTypeOption dumpType, string targetFramework, Action<string> logWarning = null)
    {
        _wasHangDumped = true;

        var processName = _processHelper.GetProcessName(processId);
        EqtTrace.Info($"ProcessDumpUtility.HangDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into temporary path '{tempDirectory}'.");

        _hangDumpDirectory = tempDirectory;

        // oh how ugly this is, but the whole infra above this starts with initializing the logger in Initialize
        // the logger needs to pass around 2 parameters, so I am just passing it around as callback instead
        _hangDumperFactory.LogWarning = logWarning;
        var dumper = _hangDumperFactory.Create(targetFramework);

        try
        {
            dumper.Dump(processId, tempDirectory, dumpType);
        }
        catch (Exception ex)
        {
            EqtTrace.Error($"Blame: Failed with error {ex}.");
            throw;
        }
    }
}
