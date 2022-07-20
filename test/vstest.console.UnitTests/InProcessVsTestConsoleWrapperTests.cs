// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Client;
using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using FluentAssertions;
using Moq;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests;

[TestClass]
public class InProcessVsTestConsoleWrapperTests
{
    private readonly InProcessVsTestConsoleWrapper _consoleWrapper;
    private readonly Mock<IEnvironmentVariableHelper> _mockEnvironmentVariableHelper;
    private readonly Mock<ITranslationLayerRequestSender> _mockRequestSender;
    private readonly Mock<ITestRequestManager> _mockTestRequestManager;
    private readonly Mock<IOutput> _mockOutput;
    private readonly Executor _executor;
    private readonly Mock<ITestPlatformEventSource> _mockEventSource;

    private readonly IList<string> _testSources = new List<String>() { "test1", "test2" };
    private readonly IList<TestCase> _testCases = new List<TestCase>() { new TestCase(), new TestCase() };
    private readonly string _runSettings = "dummy runsettings";

    public InProcessVsTestConsoleWrapperTests()
    {
        _mockEnvironmentVariableHelper = new Mock<IEnvironmentVariableHelper>();

        _mockRequestSender = new Mock<ITranslationLayerRequestSender>();
        _mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(1234);
        _mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

        _mockTestRequestManager = new Mock<ITestRequestManager>();
        _mockTestRequestManager.Setup(trm => trm.ResetOptions()).Callback(() => { });

        _mockOutput = new Mock<IOutput>();
        _executor = new Executor(_mockOutput.Object, new Mock<ITestPlatformEventSource>().Object, new ProcessHelper(), new PlatformEnvironment());
        _mockEventSource = new Mock<ITestPlatformEventSource>();

        _consoleWrapper = new InProcessVsTestConsoleWrapper(
            new ConsoleParameters(),
            _mockEnvironmentVariableHelper.Object,
            _mockRequestSender.Object,
            _mockTestRequestManager.Object,
            _executor,
            _mockEventSource.Object);
    }

    [TestMethod]
    public void InProcessWrapperConstructorShouldThrowIfPortIsInvalid()
    {
        _mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(-1);

        Assert.ThrowsException<TransationLayerException>(() =>
            new InProcessVsTestConsoleWrapper(
                new ConsoleParameters(),
                _mockEnvironmentVariableHelper.Object,
                _mockRequestSender.Object,
                _mockTestRequestManager.Object,
                new Executor(_mockOutput.Object, new Mock<ITestPlatformEventSource>().Object, new ProcessHelper(), new PlatformEnvironment()),
                new Mock<ITestPlatformEventSource>().Object));
    }

    [TestMethod]
    public void InProcessWrapperConstructorShouldSetEnvironmentVariablesReceivedAsConsoleParameters()
    {
        const string environmentVariableName = "AAAAA";

        Environment.GetEnvironmentVariable(environmentVariableName).Should().BeNull();

        var consoleParams = new ConsoleParameters();
        consoleParams.EnvironmentVariables.Add(environmentVariableName, "1");

        var _ = new InProcessVsTestConsoleWrapper(
            consoleParams,
            _mockEnvironmentVariableHelper.Object,
            _mockRequestSender.Object,
            _mockTestRequestManager.Object,
            new Executor(_mockOutput.Object, new Mock<ITestPlatformEventSource>().Object, new ProcessHelper(), new PlatformEnvironment()),
            new Mock<ITestPlatformEventSource>().Object);

        _mockEnvironmentVariableHelper.Verify(evh => evh.SetEnvironmentVariable(environmentVariableName, "1"));
    }

    [TestMethod]
    public void InProcessWrapperDiscoverTestsWithThreeParamsIsSuccessfullyInvoked()
    {
        var discoveryEventHandler = new Mock<ITestDiscoveryEventsHandler>();

        DiscoveryRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.DiscoverTests(
                It.IsAny<DiscoveryRequestPayload>(),
                It.IsAny<ITestDiscoveryEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                DiscoveryRequestPayload p,
                ITestDiscoveryEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.DiscoverTests(_testSources, _runSettings, discoveryEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.IsNull(payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo);

        _mockEventSource.Verify(es => es.TranslationLayerDiscoveryStart(), Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerDiscoveryStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.DiscoverTests(
                It.IsAny<DiscoveryRequestPayload>(),
                It.IsAny<ITestDiscoveryEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperDiscoverTestsWithFourParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();
        var discoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

        DiscoveryRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.DiscoverTests(
                It.IsAny<DiscoveryRequestPayload>(),
                It.IsAny<ITestDiscoveryEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                DiscoveryRequestPayload p,
                ITestDiscoveryEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.DiscoverTests(_testSources, _runSettings, options, discoveryEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo);

        _mockEventSource.Verify(es => es.TranslationLayerDiscoveryStart(), Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerDiscoveryStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.DiscoverTests(
                It.IsAny<DiscoveryRequestPayload>(),
                It.IsAny<ITestDiscoveryEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperDiscoverTestsWithFiveParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();
        var testSessionInfo = new TestSessionInfo();

        var discoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

        DiscoveryRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.DiscoverTests(
                It.IsAny<DiscoveryRequestPayload>(),
                It.IsAny<ITestDiscoveryEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                DiscoveryRequestPayload p,
                ITestDiscoveryEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.DiscoverTests(_testSources, _runSettings, options, testSessionInfo, discoveryEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.AreEqual(testSessionInfo.Id, payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es => es.TranslationLayerDiscoveryStart(), Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerDiscoveryStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.DiscoverTests(
                It.IsAny<DiscoveryRequestPayload>(),
                It.IsAny<ITestDiscoveryEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithSourcesWithThreeParamsIsSuccessfullyInvoked()
    {
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 _,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.RunTests(_testSources, _runSettings, runEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.IsNull(payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                0,
                0,
                _testSources.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithSourcesWithFourParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();

        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 _,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.RunTests(_testSources, _runSettings, options, runEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                0,
                0,
                _testSources.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithSourcesWithFiveParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();
        var testSessionInfo = new TestSessionInfo();

        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 _,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.RunTests(_testSources, _runSettings, options, testSessionInfo, runEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.AreEqual(testSessionInfo.Id, payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                0,
                0,
                _testSources.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithTestsWithThreeParamsIsSuccessfullyInvoked()
    {
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 _,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.RunTests(_testCases, _runSettings, runEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testCases.SequenceEqual(payload.TestCases!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.IsNull(payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                0,
                0,
                _testCases.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithTestsWithFourParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();

        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 _,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.RunTests(_testCases, _runSettings, options, runEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testCases.SequenceEqual(payload.TestCases!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                0,
                0,
                _testCases.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithTestsWithFiveParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();
        var testSessionInfo = new TestSessionInfo();

        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 _,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) => payload = p);

        _consoleWrapper.RunTests(_testCases, _runSettings, options, testSessionInfo, runEventHandler.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testCases.SequenceEqual(payload.TestCases!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.AreEqual(testSessionInfo.Id, payload.TestSessionInfo?.Id);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                0,
                0,
                _testCases.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithCustomTestHostWithSourcesWithThreeParamsIsSuccessfullyInvoked()
    {
        var testHostLauncher = new Mock<ITestHostLauncher3>();
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        ITestHostLauncher3? launcher = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 l,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) =>
            {
                payload = p;
                launcher = l;
            });

        _consoleWrapper.RunTestsWithCustomTestHost(_testSources, _runSettings, runEventHandler.Object, testHostLauncher.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.IsNull(payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);
        Assert.AreEqual(testHostLauncher.Object.IsDebug, payload.DebuggingEnabled);
        Assert.AreEqual(testHostLauncher.Object, launcher);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                1,
                _testSources.Count,
                0,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                testHostLauncher.Object,
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithCustomTestHostWithSourcesWithFourParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();

        var testHostLauncher = new Mock<ITestHostLauncher3>();
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        ITestHostLauncher3? launcher = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 l,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) =>
            {
                payload = p;
                launcher = l;
            });

        _consoleWrapper.RunTestsWithCustomTestHost(_testSources, _runSettings, options, runEventHandler.Object, testHostLauncher.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);
        Assert.AreEqual(testHostLauncher.Object.IsDebug, payload.DebuggingEnabled);
        Assert.AreEqual(testHostLauncher.Object, launcher);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                1,
                _testSources.Count,
                0,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                testHostLauncher.Object,
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithCustomTestHostWithSourcesWithFiveParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();
        var testSessionInfo = new TestSessionInfo();

        var testHostLauncher = new Mock<ITestHostLauncher3>();
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        ITestHostLauncher3? launcher = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 l,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) =>
            {
                payload = p;
                launcher = l;
            });

        _consoleWrapper.RunTestsWithCustomTestHost(_testSources, _runSettings, options, testSessionInfo, runEventHandler.Object, testHostLauncher.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testSources.SequenceEqual(payload.Sources!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.AreEqual(testSessionInfo.Id, payload.TestSessionInfo?.Id);
        Assert.AreEqual(testHostLauncher.Object.IsDebug, payload.DebuggingEnabled);
        Assert.IsNull(launcher);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                1,
                _testSources.Count,
                0,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                null,
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithCustomTestHostWithTestsWithThreeParamsIsSuccessfullyInvoked()
    {
        var testHostLauncher = new Mock<ITestHostLauncher3>();
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        ITestHostLauncher3? launcher = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 l,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) =>
            {
                payload = p;
                launcher = l;
            });

        _consoleWrapper.RunTestsWithCustomTestHost(_testCases, _runSettings, runEventHandler.Object, testHostLauncher.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testCases.SequenceEqual(payload.TestCases!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.IsNull(payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);
        Assert.AreEqual(testHostLauncher.Object.IsDebug, payload.DebuggingEnabled);
        Assert.AreEqual(testHostLauncher.Object, launcher);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                1,
                0,
                _testCases.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                testHostLauncher.Object,
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithCustomTestHostWithTestsWithFourParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();

        var testHostLauncher = new Mock<ITestHostLauncher3>();
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        ITestHostLauncher3? launcher = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 l,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) =>
            {
                payload = p;
                launcher = l;
            });

        _consoleWrapper.RunTestsWithCustomTestHost(_testCases, _runSettings, options, runEventHandler.Object, testHostLauncher.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testCases.SequenceEqual(payload.TestCases!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.IsNull(payload.TestSessionInfo?.Id);
        Assert.AreEqual(testHostLauncher.Object.IsDebug, payload.DebuggingEnabled);
        Assert.AreEqual(testHostLauncher.Object, launcher);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                1,
                0,
                _testCases.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                testHostLauncher.Object,
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    public void InProcessWrapperRunTestsWithCustomTestHostWithTestsWithFiveParamsIsSuccessfullyInvoked()
    {
        var options = new TestPlatformOptions();
        var testSessionInfo = new TestSessionInfo();

        var testHostLauncher = new Mock<ITestHostLauncher3>();
        var runEventHandler = new Mock<ITestRunEventsHandler>();

        TestRunRequestPayload? payload = null;
        ITestHostLauncher3? launcher = null;
        _mockTestRequestManager
            .Setup(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()))
            .Callback((
                TestRunRequestPayload p,
                ITestHostLauncher3 l,
                ITestRunEventsRegistrar _,
                ProtocolConfig _) =>
            {
                payload = p;
                launcher = l;
            });

        _consoleWrapper.RunTestsWithCustomTestHost(_testCases, _runSettings, options, testSessionInfo, runEventHandler.Object, testHostLauncher.Object);

        Assert.IsNotNull(payload);
        Assert.IsTrue(_testCases.SequenceEqual(payload.TestCases!));
        Assert.AreEqual(_runSettings, payload.RunSettings);
        Assert.AreEqual(options, payload.TestPlatformOptions);
        Assert.AreEqual(testSessionInfo.Id, payload.TestSessionInfo?.Id);
        Assert.AreEqual(testHostLauncher.Object.IsDebug, payload.DebuggingEnabled);
        Assert.IsNull(launcher);

        _mockEventSource.Verify(es =>
            es.TranslationLayerExecutionStart(
                1,
                0,
                _testCases.Count,
                _runSettings),
            Times.Once);
        _mockEventSource.Verify(es => es.TranslationLayerExecutionStop(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.RunTests(
                It.IsAny<TestRunRequestPayload>(),
                null,
                It.IsAny<ITestRunEventsRegistrar>(),
                It.IsAny<ProtocolConfig>()), Times.Once);
    }

    [TestMethod]
    [Obsolete("Test uses obsolete API")]
    public void InProcessWrapperStartTestSessionSucceedsWhenNoExceptionIsThrown()
    {
        var mockTestSessionEventsHandler = new Mock<ITestSessionEventsHandler>();
        mockTestSessionEventsHandler
            .Setup(eh => eh.HandleStartTestSessionComplete(It.IsAny<StartTestSessionCompleteEventArgs>()))
            .Callback(() => { });

        var testSessionInfo = new TestSessionInfo();
        var startTestSessionCompleteArgs = new StartTestSessionCompleteEventArgs()
        {
            TestSessionInfo = testSessionInfo
        };

        var stopTestSessionCompleteArgs = new StopTestSessionCompleteEventArgs()
        {
            IsStopped = true,
            TestSessionInfo = testSessionInfo
        };

        _mockTestRequestManager.Setup(trm =>
            trm.StartTestSession(
                It.IsAny<StartTestSessionPayload>(),
                It.IsAny<ITestHostLauncher3>(),
                It.IsAny<InProcessTestSessionEventsHandler>(),
                It.IsAny<ProtocolConfig>()))
            .Callback<StartTestSessionPayload, ITestHostLauncher3, ITestSessionEventsHandler, ProtocolConfig>((
                StartTestSessionPayload _,
                ITestHostLauncher3 _,
                ITestSessionEventsHandler eventsHandler,
                ProtocolConfig _) =>
                    eventsHandler.HandleStartTestSessionComplete(startTestSessionCompleteArgs));

        _mockTestRequestManager.Setup(trm =>
            trm.StopTestSession(
                It.IsAny<StopTestSessionPayload>(),
                It.IsAny<InProcessTestSessionEventsHandler>(),
                It.IsAny<ProtocolConfig>()))
            .Callback<StopTestSessionPayload, ITestSessionEventsHandler, ProtocolConfig>((
                StopTestSessionPayload _,
                ITestSessionEventsHandler eventsHandler,
                ProtocolConfig _) =>
                    eventsHandler.HandleStopTestSessionComplete(stopTestSessionCompleteArgs));

        var consoleWrapper = new InProcessVsTestConsoleWrapper(
            new ConsoleParameters(),
            _mockEnvironmentVariableHelper.Object,
            _mockRequestSender.Object,
            _mockTestRequestManager.Object,
            new Executor(_mockOutput.Object, new Mock<ITestPlatformEventSource>().Object, new ProcessHelper(), new PlatformEnvironment()),
            new Mock<ITestPlatformEventSource>().Object);

        using (var testSession = consoleWrapper?.StartTestSession(_testSources, _runSettings, mockTestSessionEventsHandler.Object))
        {
            Assert.AreEqual(
                testSession?.TestSessionInfo,
                testSessionInfo);
        }

        _mockTestRequestManager.Verify(trm => trm.ResetOptions(), Times.Exactly(2));
        _mockTestRequestManager.Verify(trm => trm.StartTestSession(
             It.IsAny<StartTestSessionPayload>(),
             It.IsAny<ITestHostLauncher3>(),
             It.IsAny<InProcessTestSessionEventsHandler>(),
             It.IsAny<ProtocolConfig>()), Times.Once);
        _mockTestRequestManager.Verify(trm => trm.StopTestSession(
             It.IsAny<StopTestSessionPayload>(),
             It.IsAny<InProcessTestSessionEventsHandler>(),
             It.IsAny<ProtocolConfig>()), Times.Once);

        mockTestSessionEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(startTestSessionCompleteArgs), Times.Once);
        mockTestSessionEventsHandler.Verify(eh => eh.HandleStopTestSessionComplete(stopTestSessionCompleteArgs), Times.Once);
    }
}
