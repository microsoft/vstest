// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Execution;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Handle to the framework which is passed to the test executors.
    /// </summary>
    internal class FrameworkHandle : TestExecutionRecorder, IFrameworkHandle2, IDisposable
    {
        /// <summary>
        /// boolean that gives the value of EnableShutdownAfterTestRun.
        /// Default value is set to false in the constructor.
        /// </summary>
        private bool enableShutdownAfterTestRun;

        /// <summary>
        /// Context in which the current run is executing.
        /// </summary>
        private TestExecutionContext testExecutionContext;

        /// <summary>
        /// DebugLauncher for launching additional adapter processes under debugger
        /// </summary>
        private ITestRunEventsHandler testRunEventsHandler;

        /// <summary>
        /// Specifies whether the handle is disposed or not
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameworkHandle"/> class.
        /// </summary>
        /// <param name="testCaseEventsHandler"> The test case level events handler. </param>
        /// <param name="testRunCache"> The test run cache. </param>
        /// <param name="testExecutionContext"> The test execution context. </param>
        /// <param name="testRunEventsHandler">TestRun Events Handler</param>
        public FrameworkHandle(ITestCaseEventsHandler testCaseEventsHandler, ITestRunCache testRunCache,
            TestExecutionContext testExecutionContext, ITestRunEventsHandler testRunEventsHandler)
            : base(testCaseEventsHandler, testRunCache)
        {
            this.testExecutionContext = testExecutionContext;
            this.testRunEventsHandler = testRunEventsHandler;
        }


        /// <summary>
        /// Give a hint to the execution framework to enable the shutdown of execution process after the test run is complete. This should be used only in out of process test runs when IRunContext.KeepAlive is true
        /// and should be used only when absolutely required as using it degrades the performance of the subsequent run.
        /// It throws InvalidOperationException when it is attempted to be enabled when keepAlive is false.
        /// </summary>
        public bool EnableShutdownAfterTestRun
        {
            get
            {
                return this.enableShutdownAfterTestRun;
            }

            set
            {
                this.enableShutdownAfterTestRun = value;
            }
        }

        /// <summary>
        /// Launch the specified process with the debugger attached.
        /// </summary>
        /// <param name="filePath">File path to the exe to launch.</param>
        /// <param name="workingDirectory">Working directory that process should use.</param>
        /// <param name="arguments">Command line arguments the process should be launched with.</param>
        /// <param name="environmentVariables">Environment variables to be set in target process</param>
        /// <returns>Process ID of the started process.</returns>
        public int LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables)
        {
            // If an adapter attempts to launch a process after the run is complete (=> this object is disposed)
            // throw an error. 
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("IFrameworkHandle");
            }

            // If it is not a debug run, then throw an error
            if (!this.testExecutionContext.IsDebug)
            {
                throw new InvalidOperationException(CrossPlatEngineResources.LaunchDebugProcessNotAllowedForANonDebugRun);
            }

            var processInfo = new TestProcessStartInfo()
            {
                Arguments = arguments,
                EnvironmentVariables = environmentVariables,
                FileName = filePath,
                WorkingDirectory = workingDirectory
            };

            return this.testRunEventsHandler.LaunchProcessWithDebuggerAttached(processInfo);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            return ((ITestRunEventsHandler2)this.testRunEventsHandler).AttachDebuggerToProcess(pid);
        }

        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this valueType implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // If you need thread safety, use a lock around these
            // operations, as well as in your methods that use the resource.
            if (!this.isDisposed)
            {
                // Indicate that the instance has been disposed.
                this.isDisposed = true;
            }
        }
    }
}
