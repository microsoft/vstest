// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using ObjectModelCommonResources = Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources.CommonResources;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging;

/// <summary>
/// The test session message logger.
/// </summary>
internal class TestSessionMessageLogger : OverheadLogger, IMessageLogger
{
    private static TestSessionMessageLogger s_instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestSessionMessageLogger"/> class.
    /// </summary>
    protected TestSessionMessageLogger()
    {
        TreatTestAdapterErrorsAsWarnings = Constants.DefaultTreatTestAdapterErrorsAsWarnings;
    }

    /// <summary>
    /// Raised when a discovery message is received.
    /// </summary>
    internal event EventHandler<TestRunMessageEventArgs> TestRunMessage;

    /// <summary>
    /// Gets the instance of the singleton.
    /// </summary>
    public static TestSessionMessageLogger Instance
    {
        get
        {
            return s_instance ??= new TestSessionMessageLogger();
        }
        set
        {
            s_instance = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to treat test adapter errors as warnings.
    /// </summary>
    internal bool TreatTestAdapterErrorsAsWarnings
    {
        get;
        set;
    }

    /// <summary>
    /// Sends a message to all listeners.
    /// </summary>
    /// <param name="testMessageLevel">Level of the message.</param>
    /// <param name="message">The message to be sent.</param>
    public void SendMessage(TestMessageLevel testMessageLevel, string message)
    {
        try
        {
            CallbackOverhead.Start();
            if (message.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(ObjectModelCommonResources.CannotBeNullOrEmpty, nameof(message));
            }

            if (TreatTestAdapterErrorsAsWarnings
                && testMessageLevel == TestMessageLevel.Error)
            {
                // Downgrade the message severity to Warning...
                testMessageLevel = TestMessageLevel.Warning;
            }

            if (TestRunMessage != null)
            {
                var args = new TestRunMessageEventArgs(testMessageLevel, message);
                TestRunMessage.SafeInvoke(this, args, "TestRunMessageLoggerProxy.SendMessage");
            }
        }
        finally
        {
            CallbackOverhead.Stop();
        }
    }
}

internal class OverheadLogger
{
    /// <summary>
    /// Measures time spent in callbacks to TestPlatform. It should not include time spent in constructors, because it is TP that is constructing this object, so we are already accounting for that time.
    /// </summary>
    public Stopwatch CallbackOverhead { get; } = new();
}
