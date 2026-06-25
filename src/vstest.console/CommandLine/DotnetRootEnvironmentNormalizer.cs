// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine;

/// <summary>
/// Normalizes the architecture-less <c>DOTNET_ROOT</c> environment variable of the current (vstest.console) process
/// once at startup, so the testhost child processes that vstest.console launches later inherit a safe value
/// regardless of their architecture.
///
/// When <c>vstest.console.exe</c> is invoked directly - e.g. by Visual Studio or by hand - rather than through
/// <c>dotnet test</c>, the SDK does not provide the <c>VSTEST_DOTNET_ROOT_PATH</c> hint that disambiguates the
/// runtime location. If the environment then has an architecture-less <c>DOTNET_ROOT</c> pointing at, say, an x64
/// installation, an x86 testhost apphost would pick it up and try to load the x64 <c>hostfxr.dll</c> into the 32-bit
/// process, failing with <c>0x800700C1</c> (<c>ERROR_BAD_EXE_FORMAT</c>). See
/// https://github.com/microsoft/vstest/issues/16151.
///
/// To avoid this, whenever we can resolve the architecture of the dotnet installation <c>DOTNET_ROOT</c> points at,
/// we promote it to the architecture specific <c>DOTNET_ROOT_&lt;ARCH&gt;</c> variable (unless already set) and clear
/// the architecture-less <c>DOTNET_ROOT</c>. Doing this once on the current process environment means every testhost
/// (of any architecture) inherits the corrected variables, instead of doing it per testhost.
/// </summary>
internal class DotnetRootEnvironmentNormalizer
{
    private const string DotnetRoot = "DOTNET_ROOT";
    private const string VsTestDotnetRootPath = "VSTEST_DOTNET_ROOT_PATH";

    private readonly IEnvironmentVariableHelper _environmentVariableHelper;
    private readonly IFileHelper _fileHelper;

    public DotnetRootEnvironmentNormalizer()
        : this(new EnvironmentVariableHelper(), new FileHelper())
    {
    }

    internal DotnetRootEnvironmentNormalizer(IEnvironmentVariableHelper environmentVariableHelper)
        : this(environmentVariableHelper, new FileHelper())
    {
    }

    internal DotnetRootEnvironmentNormalizer(IEnvironmentVariableHelper environmentVariableHelper, IFileHelper fileHelper)
    {
        _environmentVariableHelper = environmentVariableHelper;
        _fileHelper = fileHelper;
    }

    /// <summary>
    /// Normalizes the current process <c>DOTNET_ROOT</c> so it is inherited safely by testhost child processes.
    /// Must be called once at startup, before any testhost is launched.
    /// </summary>
    internal void NormalizeDotnetRootForChildProcesses()
    {
        // When the SDK launches us (dotnet test) it provides VSTEST_DOTNET_ROOT_PATH and sets the architecture
        // specific DOTNET_ROOT_<ARCH> for the testhost itself, so there is nothing for us to normalize.
        if (!StringUtils.IsNullOrWhiteSpace(_environmentVariableHelper.GetEnvironmentVariable(VsTestDotnetRootPath)))
        {
            return;
        }

        // The architecture-less DOTNET_ROOT ambiguity (an apphost of a given architecture picking up a DOTNET_ROOT
        // that points at a different architecture) and the PE based architecture probe below are Windows only.
        if (!IsWindows)
        {
            return;
        }

        var dotnetRoot = _environmentVariableHelper.GetEnvironmentVariable(DotnetRoot);
        if (StringUtils.IsNullOrWhiteSpace(dotnetRoot))
        {
            // Nothing ambiguous to normalize, let the apphost resolve the runtime on its own (registry / default
            // install location).
            return;
        }

        var muxerPath = Path.Combine(dotnetRoot, "dotnet.exe");
        var dotnetRootArchitecture = GetExecutableArchitecture(muxerPath);
        if (dotnetRootArchitecture is null)
        {
            // We could not tell which architecture DOTNET_ROOT points at, so we cannot safely make it architecture
            // specific. Leave it untouched.
            EqtTrace.Verbose($"DotnetRootEnvironmentNormalizer: Could not determine the architecture of the dotnet installation at DOTNET_ROOT='{dotnetRoot}', leaving DOTNET_ROOT untouched.");
            return;
        }

        // Promote the architecture-less DOTNET_ROOT to the architecture specific variable for the architecture it
        // actually points at, unless that variable is already set (then we trust the existing value). This keeps the
        // (possibly private) installation reachable for processes of that architecture.
        var dotnetRootArchVariable = $"DOTNET_ROOT_{dotnetRootArchitecture}";
        if (StringUtils.IsNullOrWhiteSpace(_environmentVariableHelper.GetEnvironmentVariable(dotnetRootArchVariable)))
        {
            _environmentVariableHelper.SetEnvironmentVariable(dotnetRootArchVariable, dotnetRoot);
            EqtTrace.Verbose($"DotnetRootEnvironmentNormalizer: Promoting architecture-less DOTNET_ROOT to {dotnetRootArchVariable}={dotnetRoot}.");
        }

        // Clear the ambiguous architecture-less DOTNET_ROOT so a testhost apphost of a different architecture does not
        // pick it up and load a mismatched hostfxr. Setting it to empty is treated by the host as not set.
        _environmentVariableHelper.SetEnvironmentVariable(DotnetRoot, string.Empty);
        EqtTrace.Verbose("DotnetRootEnvironmentNormalizer: Cleared architecture-less DOTNET_ROOT for testhost child processes.");
    }

    /// <summary>
    /// Whether the current process runs on Windows. Overridable for testing.
    /// </summary>
    internal virtual bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets the architecture suffix (e.g. <c>X64</c>, <c>X86</c>, <c>ARM64</c>) of a Windows PE executable
    /// (e.g. the dotnet muxer) by reading its COFF header, suitable for building a <c>DOTNET_ROOT_&lt;ARCH&gt;</c>
    /// variable name. Returns <see langword="null"/> when the architecture cannot be determined (e.g. file missing or
    /// not a Windows PE).
    /// </summary>
    internal virtual string? GetExecutableArchitecture(string executablePath)
    {
        if (!_fileHelper.Exists(executablePath))
        {
            return null;
        }

        try
        {
            // Open with FileShare.Read so the probe is resilient when the muxer (dotnet.exe) is currently running,
            // which is common on dev boxes and CI.
            using var stream = _fileHelper.GetStream(executablePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var peReader = new PEReader(stream);
            return peReader.PEHeaders.CoffHeader.Machine switch
            {
                // Only map AMD64 to X64. IA64 (Itanium) is a distinct architecture, not x64, so returning null for it
                // (and any other unknown machine) avoids setting an incorrect DOTNET_ROOT_X64 that would reintroduce
                // an architecture mismatch.
                Machine.Amd64 => "X64",
                Machine.Arm64 => "ARM64",
                Machine.Arm => "ARM",
                Machine.I386 => "X86",
                _ => null,
            };
        }
        catch (Exception ex)
        {
            EqtTrace.Verbose($"DotnetRootEnvironmentNormalizer.GetExecutableArchitecture: Failed to read architecture from '{executablePath}': {ex.Message}");
            return null;
        }
    }
}
