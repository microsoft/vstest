// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System.Collections.Generic;
using System.Threading;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

/// <summary>
/// Interface contract for handling test run events during run operation.
/// </summary>
public interface ITestRunEventsHandler2 : ITestRunEventsHandler
{
    /// <summary>
    /// Attach debugger to an already running process.
    /// </summary>
    /// <param name="pid">Process ID of the process to which the debugger should be attached.</param>
    /// <returns><see cref="true"/> if the debugger was successfully attached to the requested process, <see cref="false"/> otherwise.</returns>
    bool AttachDebuggerToProcess(int pid);
}

/// <summary>
/// Interface contract for handling test run events during run operation.
/// </summary>
public interface ITestRunEventsHandler3 : ITestRunEventsHandler2
{
    /// <summary>
    /// Attach debugger to an already running process.
    /// </summary>
    /// <param name="pid">Process ID of the process to which the debugger should be attached.</param>
    /// <returns><see cref="true"/> if the debugger was successfully attached to the requested process, <see cref="false"/> otherwise.</returns>
    bool AttachDebuggerToProcess(AttachDebuggerInfo attachDebuggerInfo, CancellationToken cancellationToken);
}

/// <summary>
/// Interface contract for handling test run events during run operation.
/// </summary>
public interface IInternalTestRunEventsHandler : ITestMessageEventHandler
{
    /// <summary>
    /// Handle the TestRunCompletion event from a test engine
    /// </summary>
    /// <param name="testRunCompleteArgs">TestRunCompletion Data</param>
    /// <param name="lastChunkArgs">Last set of test results</param>
    /// <param name="runContextAttachments">Attachments of the test run</param>
    /// <param name="executorUris">ExecutorURIs of the adapters involved in test run</param>
    void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris);

    /// <summary>
    /// Handle a change in TestRun i.e. new testresults and stats
    /// </summary>
    /// <param name="testRunChangedArgs">TestRunChanged Data</param>
    void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs);

    /// <summary>
    /// Launches a process with a given process info under debugger
    /// Adapter get to call into this to launch any additional processes under debugger
    /// </summary>
    /// <param name="testProcessStartInfo">Process start info</param>
    /// <returns>ProcessId of the launched process</returns>
    int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo);

    /// <summary>
    /// Attach debugger to an already running process.
    /// </summary>
    /// <param name="pid">Process ID of the process to which the debugger should be attached.</param>
    /// <returns><see cref="true"/> if the debugger was successfully attached to the requested process, <see cref="false"/> otherwise.</returns>
    bool AttachDebuggerToProcess(AttachDebuggerInfo attachDebuggerInfo);
}

