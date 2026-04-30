// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

/// <summary>
/// Provides lightweight test case lifecycle notifications for real-time in-flight tracking.
/// Implemented by event handlers that can relay start/finish signals to the console.
/// </summary>
internal interface ITestCaseLifecycleNotifier
{
    void SendTestCaseStarting(TestCase testCase);

    void SendTestCaseFinished(TestCase testCase);
}
