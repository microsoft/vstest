// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

internal class AeDebuggerArgumentProcessor : IArgumentProcessor
{
    public const string CommandName = "/AeDebugger";
    private Lazy<IArgumentProcessorCapabilities>? _metadata;
    private Lazy<IArgumentExecutor>? _executor;


    public Lazy<IArgumentExecutor>? Executor
    {
        get => _executor ??= new Lazy<IArgumentExecutor>(() =>
            new AeDebuggerArgumentExecutor(new PlatformEnvironment(), new FileHelper(), new ProcessHelper(), ConsoleOutput.Instance));

        set => _executor = value;
    }

    public Lazy<IArgumentProcessorCapabilities> Metadata
        => _metadata ??= new Lazy<IArgumentProcessorCapabilities>(() => new AeDebuggerArgumentProcessorCapabilities());
}

internal class AeDebuggerArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => AeDebuggerArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => true;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

    // This feature is for internal usage at the moment, we can advertise in future when we'll have
    // good feedback on the usage.
    public override string? HelpContentResourceName => null;

    public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
}

internal class AeDebuggerArgumentExecutor : IArgumentExecutor
{
    private const int ProcDumpTimeoutSeconds = 10;
    private const string InstallCommandArgumentName = "Install";
    private const string UninstallCommandArgumentName = "Uninstall";

    private readonly IEnvironment _environment;
    private readonly IFileHelper _fileHelper;
    private readonly IProcessHelper _processHelper;
    private readonly IOutput _output;
    private string? _argument;
    private Dictionary<string, string>? _collectDumpParameters;

    public AeDebuggerArgumentExecutor(IEnvironment environment, IFileHelper fileHelper, IProcessHelper processHelper, IOutput output)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _fileHelper = fileHelper ?? throw new ArgumentNullException(nameof(fileHelper));
        _processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Initialize(string? argument) => _argument = argument;

    public ArgumentProcessorResult Execute()
    {
        string exceptionMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidAeDebuggerArgument, _argument ?? "");
        if (StringUtils.IsNullOrEmpty(_argument))
        {
            _output.Error(false, exceptionMessage);
            return ArgumentProcessorResult.Fail;
        }

        string[] aeDebuggerArgumentList = ArgumentProcessorUtilities.GetArgumentList(_argument, ArgumentProcessorUtilities.SemiColonArgumentSeparator, exceptionMessage);
        _collectDumpParameters = ArgumentProcessorUtilities.GetArgumentParameters(
            aeDebuggerArgumentList.Where(x => !x.Equals(InstallCommandArgumentName, StringComparison.OrdinalIgnoreCase) &&
            !x.Equals(UninstallCommandArgumentName, StringComparison.OrdinalIgnoreCase)),
            ArgumentProcessorUtilities.EqualNameValueSeparator, exceptionMessage);

        if (aeDebuggerArgumentList.Contains(InstallCommandArgumentName, StringComparer.OrdinalIgnoreCase))
        {
            return InstallUnistallPostmortemDebugger(true);
        }

        if (aeDebuggerArgumentList.Contains(UninstallCommandArgumentName, StringComparer.OrdinalIgnoreCase))
        {
            return InstallUnistallPostmortemDebugger(false);
        }

        _output.Error(false, exceptionMessage);
        return ArgumentProcessorResult.Fail;
    }

    private ArgumentProcessorResult InstallUnistallPostmortemDebugger(bool install)
    {
        if (_environment.OperatingSystem != PlatformOperatingSystem.Windows)
        {
            _output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.PostmortemDebuggerNotSupportedForCurrentOS));
            return ArgumentProcessorResult.Fail;
        }

        if (_collectDumpParameters is null)
        {
            _output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.ProcDumpToolDirectoryPathArgumenNotFound));
            return ArgumentProcessorResult.Fail;
        }

        // Validate ProcDumpToolDirectoryPath
        if (!TryGetDirectoryInfo(_collectDumpParameters,
            "ProcDumpToolDirectoryPath",
            CommandLineResources.ProcDumpToolDirectoryPathArgumenNotFound,
            CommandLineResources.InvalidProcDumpToolDirectoryPath,
            out DirectoryInfo? procDumpToolDirectoryPath))
        {
            return ArgumentProcessorResult.Fail;
        }

        // Looking for procdump*.exe
        FileInfo procDumpFileName = new(Path.Combine(procDumpToolDirectoryPath.FullName, ProcDumpFileName()));
        if (!_fileHelper.Exists(procDumpFileName.FullName))
        {
            _output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.ProcDumpFileNameNotFound, procDumpFileName.FullName));
            return ArgumentProcessorResult.Fail;
        }

        string procDumpInstallUnistallArgument = "";
        if (install)
        {
            // Validate ProcDumpDirectoryPath
            if (!TryGetDirectoryInfo(_collectDumpParameters,
                "DumpDirectoryPath",
                CommandLineResources.ProcDumpDirectoryPathArgumenNotFound,
                CommandLineResources.InvalidProcDumpDirectoryPath,
                out DirectoryInfo? dumpDirectoryPath))
            {
                return ArgumentProcessorResult.Fail;
            }

            procDumpInstallUnistallArgument = dumpDirectoryPath.FullName;
        }

        if (_processHelper.LaunchProcess(procDumpFileName.FullName, install ? "-ma -i" : "-u", procDumpInstallUnistallArgument, null,
            (_, data) =>
            {
                if (data is not null && !StringUtilities.IsNullOrWhiteSpace(data))
                {
                    _output.Error(false, data, null);
                }
            },
            null,
            (_, data) =>
            {
                if (data is not null && !StringUtilities.IsNullOrWhiteSpace(data))
                {
                    _output.Information(false, data, null);
                }
            })
            is Process process)
        {
            return !process.WaitForExit(TimeSpan.FromSeconds(ProcDumpTimeoutSeconds).Seconds)
                ? ArgumentProcessorResult.Fail
                : process.ExitCode == 0 ? ArgumentProcessorResult.Success : ArgumentProcessorResult.Fail;
        }

        // We suppose a success if the object returned by the LaunchProcess is not a Process object.
        return ArgumentProcessorResult.Success;

        string ProcDumpFileName() =>
            _processHelper.GetCurrentProcessArchitecture() switch
            {
                PlatformArchitecture.X86 => "procdump.exe",
                PlatformArchitecture.ARM64 => "procdump64a.exe",
                _ => "procdump64.exe",
            };

        bool TryGetDirectoryInfo(Dictionary<string, string> collectDumpParameters,
            string directoryArgumentName,
            string invalidArgumentErrorMessage,
            string invalidDirectoryErrorMessage,
            [NotNullWhen(true)] out DirectoryInfo? directoryInfo)
        {
            directoryInfo = null;

            if (!collectDumpParameters.TryGetValue(directoryArgumentName, out string? directoryPath))
            {
                _output.Error(false, string.Format(CultureInfo.CurrentCulture, invalidArgumentErrorMessage));
                return false;
            }

            if (directoryPath is null)
            {
                _output.Error(false, string.Format(CultureInfo.CurrentCulture, invalidArgumentErrorMessage));
                return false;
            }

            directoryInfo = new(directoryPath);
            if (!_fileHelper.DirectoryExists(directoryInfo.FullName))
            {
                _output.Error(false, string.Format(CultureInfo.CurrentCulture, invalidDirectoryErrorMessage, directoryInfo.FullName));
                directoryInfo = null;
                return false;
            }

            return true;
        }
    }

}
