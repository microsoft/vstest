// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests;

/// <summary>
/// Tests for <see cref="RollingFileTraceListener"/>.
/// </summary>
/// <remarks>
/// <see cref="RollingFileTraceListener.StreamWriterRollingHelper.UpdateRollingInformationIfNecessary"/>
/// used to silently swallow the exception thrown when it failed to reopen the trace file, leaving the
/// tracing subsystem with zero diagnostic trail of its own failure. It must now report the failure via
/// <see cref="Trace"/> (not <see cref="EqtTrace"/>, to avoid recursing into the failing listener) and
/// engage the same failed-roll throttle used by the rest of the class, while still returning false so
/// callers abort rolling instead of throwing.
/// </remarks>
[TestClass]
public class RollingFileTraceListenerTests
{
    private readonly RecordingTraceListener _recorder = new();

    [TestInitialize]
    public void Initialize() => Trace.Listeners.Add(_recorder);

    [TestCleanup]
    public void Cleanup() => Trace.Listeners.Remove(_recorder);

    /// <summary>
    /// When the file can be reopened, no failure is reported and the retry throttle is left untouched.
    /// </summary>
    [TestMethod]
    public void UpdateRollingInformationIfNecessaryReturnsTrueWhenWriterCanBeOpened()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            using var listener = new RollingFileTraceListener(tempFile, "test", rollSizeKB: 1);
            ClearManagedWriter(listener);

            // Force the managed writer to be recreated from scratch, as happens right after a roll.
            Assert.IsTrue(listener.RollingHelper.UpdateRollingInformationIfNecessary());
            Assert.IsEmpty(_recorder.Messages, "No diagnostic trail is expected on the success path.");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// When the trace file cannot be reopened (e.g. the directory has been removed), the listener must
    /// report the failure through <see cref="Trace"/> and engage the failed-roll throttle instead of
    /// throwing or staying silent.
    /// </summary>
    [TestMethod]
    public void UpdateRollingInformationIfNecessaryReportsFailureAndThrottlesRetriesWhenReopenFails()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        string tempFile = Path.Combine(tempDirectory, "trace.log");

        try
        {
            using var listener = new RollingFileTraceListener(tempFile, "test", rollSizeKB: 1);
            ClearManagedWriter(listener);

            // Remove the directory entirely so reopening the trace file fails, as it would right after a
            // roll if the destination directory had been deleted out from under the running process.
            Directory.Delete(tempDirectory, recursive: true);

            Assert.IsFalse(listener.RollingHelper.UpdateRollingInformationIfNecessary());
            Assert.IsTrue(
                _recorder.Messages.Exists(m => m.Contains(tempFile, StringComparison.Ordinal)),
                "The failure must be reported through Trace so a debugger/listener has a diagnostic signal.");

            // The failed-roll throttle must now be engaged: a second call within the timeout window must
            // short-circuit (returning false) without attempting - and failing - to reopen the file again.
            int messageCountAfterFirstFailure = _recorder.Messages.Count;
            Assert.IsFalse(listener.RollingHelper.UpdateRollingInformationIfNecessary());
            Assert.HasCount(
                messageCountAfterFirstFailure,
                _recorder.Messages,
                "The throttle must prevent a second reopen attempt (and a second failure report) immediately after the first.");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Clears the private managed-writer field, reproducing the state <see cref="RollingFileTraceListener.StreamWriterRollingHelper.PerformRoll"/>
    /// leaves behind right after a roll completes, which is what triggers <see cref="RollingFileTraceListener.OpenTextWriter"/>
    /// to be invoked again on the next call to <c>UpdateRollingInformationIfNecessary</c>.
    /// </summary>
    private static void ClearManagedWriter(RollingFileTraceListener listener)
    {
        FieldInfo field = typeof(RollingFileTraceListener.StreamWriterRollingHelper)
            .GetField("_managedWriter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(listener.RollingHelper, null);
    }

    private sealed class RecordingTraceListener : TraceListener
    {
        public System.Collections.Generic.List<string> Messages { get; } = new();

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message) => Messages.Add(message ?? string.Empty);
    }
}
