// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.TestPlatform.TestHostProvider.Hosting;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.TestHostProvider.Hosting.UnitTests;

[TestClass]
public class TestHostManagerCallbacksTests
{
    private StringBuilder _testHostProcessStdError;

    public TestHostManagerCallbacksTests()
    {
        _testHostProcessStdError = new StringBuilder(0, Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants.StandardErrorMaxLength);
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendNoDataOnNullDataReceived()
    {
        _testHostProcessStdError.Append("NoDataShouldAppend");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, null);

        Assert.AreEqual("NoDataShouldAppend", _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendNoDataOnEmptyDataReceived()
    {
        _testHostProcessStdError.Append("NoDataShouldAppend");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, string.Empty);

        Assert.AreEqual("NoDataShouldAppend", _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendWhiteSpaceString()
    {
        _testHostProcessStdError.Append("OldData");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, " ");

        Assert.AreEqual("OldData " + Environment.NewLine, _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendGivenData()
    {
        _testHostProcessStdError.Append("NoDataShouldAppend");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, "new data");

        Assert.AreEqual("NoDataShouldAppendnew data" + Environment.NewLine, _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldNotAppendNewDataIfErrorMessageAlreadyReachedMaxLength()
    {
        _testHostProcessStdError = new StringBuilder(0, 5);
        _testHostProcessStdError.Append("12345");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, "678");

        Assert.AreEqual("12345", _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendSubStringOfDataIfErrorMessageReachedMaxLength()
    {
        _testHostProcessStdError = new StringBuilder(0, 5);
        _testHostProcessStdError.Append("1234");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, "5678");

        Assert.AreEqual("12345", _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendEntireStringEvenItReachesToMaxLength()
    {
        _testHostProcessStdError = new StringBuilder(0, 5);
        _testHostProcessStdError.Append("12");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, "3");

        Assert.AreEqual("123" + Environment.NewLine, _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldAppendNewLineApproprioritlyWhenReachingMaxLength()
    {
        _testHostProcessStdError = new StringBuilder(0, 5);
        _testHostProcessStdError.Append("123");
        TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, "4");

        Assert.AreEqual("1234" + Environment.NewLine.Substring(0, 1), _testHostProcessStdError.ToString());
    }

    [TestMethod]
    public void ErrorReceivedCallbackShouldNotCrashIfInvalidProcessHandle()
    {
        var handleProcessHelper = new InvalidHandleProcessHelper();
        bool onHostExitedCalled = false;
        TestHostManagerCallbacks.ExitCallBack(handleProcessHelper, null, new StringBuilder(),
            hostProviderEventArgs =>
            {
                onHostExitedCalled = true;
                Assert.AreEqual(-1, hostProviderEventArgs.ErrroCode);
            });

        Assert.IsTrue(handleProcessHelper.TryGetExitCodeCalled, "TryGetExitCodeCalled was not called");
        Assert.IsTrue(onHostExitedCalled, "onHostExited was not called");
    }

    private class InvalidHandleProcessHelper : IProcessHelper
    {
        public bool TryGetExitCodeCalled { get; private set; }

        public PlatformArchitecture GetCurrentProcessArchitecture()
        {
            throw new NotImplementedException();
        }

        public string? GetCurrentProcessFileName()
        {
            throw new NotImplementedException();
        }

        public int GetCurrentProcessId()
        {
            throw new NotImplementedException();
        }

        public string GetCurrentProcessLocation()
        {
            throw new NotImplementedException();
        }

        public string GetNativeDllDirectory()
        {
            throw new NotImplementedException();
        }

        public PlatformArchitecture GetProcessArchitecture(int processId)
        {
            throw new NotImplementedException();
        }

        public nint GetProcessHandle(int processId)
        {
            throw new NotImplementedException();
        }

        public int GetProcessId(object? process)
        {
            throw new NotImplementedException();
        }

        public string GetProcessName(int processId)
        {
            throw new NotImplementedException();
        }

        public string? GetTestEngineDirectory()
        {
            throw new NotImplementedException();
        }

        public object LaunchProcess(string processPath, string? arguments, string? workingDirectory, IDictionary<string, string?>? envVariables, Action<object?, string?>? errorCallback, Action<object?>? exitCallBack, Action<object?, string?>? outputCallBack)
        {
            throw new NotImplementedException();
        }

        public void SetExitCallback(int processId, Action<object?>? callbackAction)
        {
            throw new NotImplementedException();
        }

        public void TerminateProcess(object? process)
        {
            throw new NotImplementedException();
        }

        public bool TryGetExitCode(object? process, out int exitCode)
        {
            var err = new COMException("Invalid handle", unchecked((int)0x80070006));
            typeof(COMException).GetProperty("HResult")!.SetValue(err, unchecked((int)0x80070006));
            TryGetExitCodeCalled = true;
            throw err;
        }

        public void WaitForProcessExit(object? process)
        {
            throw new NotImplementedException();
        }
    }
}
