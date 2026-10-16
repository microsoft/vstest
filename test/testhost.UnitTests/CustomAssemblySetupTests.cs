// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

using System;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace testhost.UnitTests;

/// <summary>
/// Tests for <see cref="CustomAssemblySetup"/>'s assembly-resolve fallback loop.
/// </summary>
/// <remarks>
/// The <c>foreach (var path in _resolverPaths)</c> loop in <c>CurrentDomain_AssemblyResolve</c> used to
/// swallow a failed <c>Assembly.LoadFrom</c> call with a bare <c>catch (Exception) {{ // ignore }}</c>,
/// so when assembly resolution ultimately failed for every candidate path, there was no diagnostic trail
/// of which path(s) were tried or why each one failed - only the final "Failed to resolve" summary line.
/// The fix adds an <see cref="EqtTrace"/> call inside that catch so each per-path failure is recorded.
/// </remarks>
[TestClass]
public class CustomAssemblySetupTests
{
    private static string? s_logFile;

    [ClassInitialize]
    public static void Init(TestContext _)
    {
        EqtTrace.DoNotInitailize = false;
        string dirPath = Path.Combine(Path.GetTempPath(), "CustomAssemblySetupTests");
        Directory.CreateDirectory(dirPath);
        s_logFile = Path.Combine(dirPath, "trace.log");
        EqtTrace.InitializeVerboseTrace(s_logFile);
    }

    /// <summary>
    /// A candidate resolver path that contains a file matching the requested assembly name, but whose
    /// content is not a valid assembly, must still let resolution continue to other candidates (and
    /// ultimately report failure) while recording the per-candidate load failure for diagnosability.
    /// </summary>
    [TestMethod]
    public void AssemblyResolveLogsPerPathFailureWhenLoadFromThrows()
    {
        string resolverDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(resolverDirectory);
        string assemblyName = "Not.A.Real.Assembly." + Path.GetRandomFileName().Replace(".", "");
        string badAssemblyFile = Path.Combine(resolverDirectory, assemblyName + ".dll");

        try
        {
            // Not a valid PE/assembly file: Assembly.LoadFrom will throw BadImageFormatException.
            File.WriteAllText(badAssemblyFile, "this is not a valid assembly");

            var setup = new CustomAssemblySetup(null!, resolverDirectory);
            MethodInfo resolveMethod = typeof(CustomAssemblySetup).GetMethod(
                "CurrentDomain_AssemblyResolve",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

            var resolveEventArgs = new ResolveEventArgs(assemblyName);
            Assembly? resolved = (Assembly?)resolveMethod.Invoke(setup, [this, resolveEventArgs]);

            Assert.IsNull(resolved, "The malformed assembly cannot be loaded, so resolution must fail.");

            string logContent = File.ReadAllText(s_logFile!);
            Assert.Contains(
                badAssemblyFile,
                logContent,
                "The per-path load failure must be traced with the attempted file path so /diag logs show which candidate(s) were tried.");
        }
        finally
        {
            Directory.Delete(resolverDirectory, recursive: true);
        }
    }
}
#endif
