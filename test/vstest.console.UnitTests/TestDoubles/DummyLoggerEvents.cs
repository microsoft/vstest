﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.console.UnitTests.TestDoubles;
#pragma warning restore IDE1006 // Naming Styles

using System;

using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

internal class DummyLoggerEvents : InternalTestLoggerEvents
{
    public DummyLoggerEvents(TestSessionMessageLogger testSessionMessageLogger) : base(testSessionMessageLogger)
    {
    }

    public override event EventHandler<TestResultEventArgs> TestResult;
    public override event EventHandler<TestRunCompleteEventArgs> TestRunComplete;
    public override event EventHandler<TestRunMessageEventArgs> TestRunMessage;

    public bool EventsSubscribed()
    {
        return TestResult != null && TestRunComplete != null && TestRunMessage != null;
    }
}