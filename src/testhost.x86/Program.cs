// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHost;

using System;
using System.Collections.Generic;

using CoreUtilities.Helpers;
using CoreUtilities.Tracing;

using Execution;

using ObjectModel;

/// <summary>
/// The program.
/// </summary>
public class Program
{
    private const string TestSourceArgumentString = "--testsourcepath";

    /// <summary>
    /// The main.
    /// </summary>
    /// <param name="args">
    /// The args.
    /// </param>
    public static void Main(string[] args)
    {
        try
        {
            TestPlatformEventSource.Instance.TestHostStart();
            Run(args);
        }
        catch (Exception ex)
        {
            EqtTrace.Error("TestHost: Error occurred during initialization of TestHost : {0}", ex);

            // Throw exception so that vstest.console get the exception message.
            throw;
        }
        finally
        {
            TestPlatformEventSource.Instance.TestHostStop();
            EqtTrace.Info("Testhost process exiting.");
        }
    }

    // In UWP(App models) Run will act as entry point from Application end, so making this method public
    public static void Run(string[] args)
    {
        DebuggerBreakpoint.AttachVisualStudioDebugger("VSTEST_HOST_DEBUG_ATTACHVS");
        DebuggerBreakpoint.WaitForNativeDebugger("VSTEST_HOST_NATIVE_DEBUG");
        DebuggerBreakpoint.WaitForDebugger("VSTEST_HOST_DEBUG");
        UiLanguageOverride.SetCultureSpecifiedByUser();
        var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args);

        // Invoke the engine with arguments
        GetEngineInvoker(argsDictionary).Invoke(argsDictionary);
    }

    private static IEngineInvoker GetEngineInvoker(IDictionary<string, string> argsDictionary)
    {
        IEngineInvoker invoker = null;
#if NETFRAMEWORK
        // If Args contains test source argument, invoker Engine in new appdomain
        if (argsDictionary.TryGetValue(TestSourceArgumentString, out var testSourcePath) && !string.IsNullOrWhiteSpace(testSourcePath))
        {
            // remove the test source arg from dictionary
            argsDictionary.Remove(TestSourceArgumentString);

            // Only DLLs and EXEs can have app.configs or ".exe.config" or ".dll.config"
            if (System.IO.File.Exists(testSourcePath) &&
                (testSourcePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                 || testSourcePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
            {
                invoker = new AppDomainEngineInvoker<DefaultEngineInvoker>(testSourcePath);
            }
        }
#endif
        return invoker ?? new DefaultEngineInvoker();
    }
}
