// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests;

using System;
using System.Diagnostics;

using CommunicationUtilities.DataCollection.Interfaces;

using CoreUtilities.Helpers;

using ObjectModel;
using TestTools.UnitTesting;

using Moq;

using PlatformAbstractions.Interfaces;

using TestPlatform.DataCollector;

[TestClass]
public class DataCollectorMainTests
{
    private readonly string[] _args = { "--port", "1025", "--parentprocessid", "100", "--diag", "abc.txt", "--tracelevel", "3" };
    private readonly string[] _argsWithEmptyDiagArg = { "--port", "1025", "--parentprocessid", "100", "--diag", "", "--tracelevel", "3" };
    private readonly string[] _argsWithInvalidTraceLevel = { "--port", "1025", "--parentprocessid", "100", "--diag", "abc.txt", "--tracelevel", "5" };

    private static readonly string TimoutErrorMessage =
        "datacollector process failed to connect to vstest.console process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";
    private readonly Mock<IProcessHelper> _mockProcessHelper;
    private readonly Mock<IEnvironment> _mockEnvironment;
    private readonly Mock<IDataCollectionRequestHandler> _mockDataCollectionRequestHandler;
    private readonly DataCollectorMain _dataCollectorMain;

    public DataCollectorMainTests()
    {
        _mockProcessHelper = new Mock<IProcessHelper>();
        _mockEnvironment = new Mock<IEnvironment>();
        _mockDataCollectionRequestHandler = new Mock<IDataCollectionRequestHandler>();
        _dataCollectorMain = new DataCollectorMain(_mockProcessHelper.Object, _mockEnvironment.Object, _mockDataCollectionRequestHandler.Object);
        _mockDataCollectionRequestHandler.Setup(rh => rh.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(true);
    }

    [TestCleanup]
    public void CleanUp()
    {
        Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "");
    }

    [TestMethod]
    public void RunShouldTimeoutBasedOnEnvVariable()
    {
        Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "10");
        _dataCollectorMain.Run(_args);
        _mockDataCollectionRequestHandler.Verify(rh => rh.WaitForRequestSenderConnection(10 * 1000));
    }

    [TestMethod]
    public void RunShouldTimeoutBasedDefaulValueIfEnvVariableNotSet()
    {
        _dataCollectorMain.Run(_args);

        _mockDataCollectionRequestHandler.Verify(rh => rh.WaitForRequestSenderConnection(EnvironmentHelper.DefaultConnectionTimeout * 1000));
    }

    [TestMethod]
    public void RunShouldInitializeTraceWithTraceLevelOffIfDiagArgIsEmpty()
    {
        // Setting EqtTrace.TraceLevel to a value other than info.
#if NETFRAMEWORK
        EqtTrace.TraceLevel = TraceLevel.Verbose;
#else
        EqtTrace.TraceLevel = PlatformTraceLevel.Verbose;
#endif
        // Action
        _dataCollectorMain.Run(_argsWithEmptyDiagArg); // Passing tracelevel as info and diag file path is empty.

        // Verify
        Assert.AreEqual(TraceLevel.Off, (TraceLevel)EqtTrace.TraceLevel);
    }

    [TestMethod]
    public void RunShouldInitializeTraceWithVerboseTraceLevelIfInvalidTraceLevelPassed()
    {
        // Setting EqtTrace.TraceLevel to a value other than info.
#if NETFRAMEWORK
        EqtTrace.TraceLevel = TraceLevel.Info;
#else
        EqtTrace.TraceLevel = PlatformTraceLevel.Info;
#endif
        // Action
        _dataCollectorMain.Run(_argsWithInvalidTraceLevel);

        // Verify
        Assert.AreEqual(TraceLevel.Verbose, (TraceLevel)EqtTrace.TraceLevel);
    }

    [TestMethod]
    public void RunShouldInitializeTraceWithCorrectVerboseTraceLevel()
    {
        // Setting EqtTrace.TraceLevel to a value other than info.
#if NETFRAMEWORK
        EqtTrace.TraceLevel = TraceLevel.Verbose;
#else
        EqtTrace.TraceLevel = PlatformTraceLevel.Verbose;
#endif
        // Action
        _dataCollectorMain.Run(_args); // Trace level is set as info in args.

        // Verify
        Assert.AreEqual(TraceLevel.Info, (TraceLevel)EqtTrace.TraceLevel);
    }

    [TestMethod]
    public void RunShouldThrowIfTimeoutOccured()
    {
        _mockDataCollectionRequestHandler.Setup(rh => rh.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(false);
        var message = Assert.ThrowsException<TestPlatformException>(() => _dataCollectorMain.Run(_args)).Message;
        Assert.AreEqual(TimoutErrorMessage, message);
    }
}
