// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using global::TestPlatform.TestUtilities;

    internal sealed class ObservedProcessManager : IProcessManager
    {
        private readonly VsTestConsoleProcessManager inner;
        private readonly ProcessInvocation invocation;

        internal ObservedProcessManager(string consolePath, string dotnetPath, ProcessInvocation invocation)
        {
            this.inner = new VsTestConsoleProcessManager(consolePath, dotnetPath);
            this.invocation = invocation;
        }

        public event EventHandler ProcessExited
        {
            add { this.inner.ProcessExited += value; }
            remove { this.inner.ProcessExited -= value; }
        }

        public bool IsProcessInitialized() => this.inner.IsProcessInitialized();
        public void ShutdownProcess() => this.inner.ShutdownProcess();

        public void StartProcess(ConsoleParameters parameters)
        {
            this.inner.StartProcess(parameters);
            // This old process-manager interface exposes no PID. Read the actual owned Process in
            // test code rather than guessing from machine-wide names/timestamps or changing production APIs.
            var field = typeof(VsTestConsoleProcessManager).GetField("process", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(field?.GetValue(this.inner) is Process process))
            {
                throw new InvalidOperationException("The test observer could not access the launched runner Process.");
            }
            this.invocation.SetRoot(process);
        }
    }
}
