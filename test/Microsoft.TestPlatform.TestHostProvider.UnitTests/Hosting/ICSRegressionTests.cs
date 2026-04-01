// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using Microsoft.TestPlatform.TestHostProvider.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace TestPlatform.TestHostProvider.Hosting.UnitTests;

/// <summary>
/// Regression tests for:
/// - Issue #5184 / PR #5192: stderr forwarded as Error instead of Informational.
/// - Issue #2473 / PR #2479: ARM64 architecture unhandled exception.
/// </summary>
[TestClass]
public class ICSRegressionTests
{
    private readonly StringBuilder _testHostProcessStdError;

    public ICSRegressionTests()
    {
        _testHostProcessStdError = new StringBuilder(0, Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants.StandardErrorMaxLength);
    }

    [TestMethod]
    public void ErrorReceivedCallback_WithForwardOutput_ShouldSendAsInformational()
    {
        // Arrange
        var mockLogger = new Mock<IMessageLogger>();
        var callbacks = new TestHostManagerCallbacks(true, mockLogger.Object);
        var stderrData = "Some debug output from test host";

        // Act
        callbacks.ErrorReceivedCallback(_testHostProcessStdError, stderrData);

        // Assert: the message should be forwarded as Informational, NOT Error.
        mockLogger.Verify(
            l => l.SendMessage(TestMessageLevel.Informational, stderrData),
            Times.Once(),
            "Stderr output should be forwarded as Informational.");

        mockLogger.Verify(
            l => l.SendMessage(TestMessageLevel.Error, It.IsAny<string>()),
            Times.Never(),
            "Stderr output should NOT be forwarded as Error.");
    }

    [TestMethod]
    public void ErrorReceivedCallback_WithForwardOutputDisabled_ShouldNotSendAnyMessage()
    {
        // Arrange
        var mockLogger = new Mock<IMessageLogger>();
        var callbacks = new TestHostManagerCallbacks(false, mockLogger.Object);

        // Act
        callbacks.ErrorReceivedCallback(_testHostProcessStdError, "stderr data");

        // Assert: no message should be sent when forwarding is disabled.
        mockLogger.Verify(
            l => l.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()),
            Times.Never(),
            "No message should be sent when forwardOutput is false.");
    }

    [TestMethod]
    public void ErrorReceivedCallback_WithNullData_ShouldNotSendMessage()
    {
        // Arrange
        var mockLogger = new Mock<IMessageLogger>();
        var callbacks = new TestHostManagerCallbacks(true, mockLogger.Object);

        // Act
        callbacks.ErrorReceivedCallback(_testHostProcessStdError, null);

        // Assert: null data should not trigger message forwarding.
        mockLogger.Verify(
            l => l.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()),
            Times.Never(),
            "Null data should not be forwarded.");
    }

    [TestMethod]
    public void ErrorReceivedCallback_WithWhitespaceData_ShouldNotSendMessage()
    {
        // Arrange
        var mockLogger = new Mock<IMessageLogger>();
        var callbacks = new TestHostManagerCallbacks(true, mockLogger.Object);

        // Act
        callbacks.ErrorReceivedCallback(_testHostProcessStdError, "   ");

        // Assert: whitespace-only data should not trigger message forwarding.
        mockLogger.Verify(
            l => l.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()),
            Times.Never(),
            "Whitespace-only data should not be forwarded.");
    }

    [TestMethod]
    public void ErrorReceivedCallback_MultipleMessages_ShouldAllBeInformational()
    {
        // Arrange
        var mockLogger = new Mock<IMessageLogger>();
        var callbacks = new TestHostManagerCallbacks(true, mockLogger.Object);

        // Act
        callbacks.ErrorReceivedCallback(_testHostProcessStdError, "First error line");
        callbacks.ErrorReceivedCallback(_testHostProcessStdError, "Second error line");

        // Assert: all messages should be Informational.
        mockLogger.Verify(
            l => l.SendMessage(TestMessageLevel.Informational, It.IsAny<string>()),
            Times.Exactly(2),
            "All stderr messages should be forwarded as Informational.");

        mockLogger.Verify(
            l => l.SendMessage(TestMessageLevel.Error, It.IsAny<string>()),
            Times.Never(),
            "No stderr messages should be forwarded as Error.");
    }

    #region Issue #2473 - ARM64 unhandled exception

    [TestMethod]
    public void Architecture_ARM64_ShouldBeDefined()
    {
        // Before the fix, Architecture.ARM64 was not handled in switch statements,
        // causing unhandled exceptions on ARM64 Windows.
        Assert.IsTrue(Enum.IsDefined(typeof(Architecture), Architecture.ARM64),
            "Architecture.ARM64 should be defined (Issue #2473).");
    }

    [TestMethod]
    public void PlatformArchitecture_ARM64_ShouldBeDefined()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(PlatformArchitecture), PlatformArchitecture.ARM64),
            "PlatformArchitecture.ARM64 should be defined to support ARM64 architecture mapping.");
    }

    [TestMethod]
    public void Architecture_ARM64_ShouldHaveExpectedIntegerValue()
    {
        // Verify ARM64 has a stable enum value for serialization/mapping.
        var arm64Value = (int)Architecture.ARM64;
        Assert.AreEqual(5, arm64Value,
            "Architecture.ARM64 should have integer value 5.");
    }

    [TestMethod]
    public void PlatformArchitecture_ARM64_ShouldHaveExpectedIntegerValue()
    {
        var arm64Value = (int)PlatformArchitecture.ARM64;
        Assert.AreEqual(3, arm64Value,
            "PlatformArchitecture.ARM64 should have integer value 3.");
    }

    [TestMethod]
    public void Architecture_ARM64_NameShouldBeCorrect()
    {
        // Verify the enum name serializes correctly.
        Assert.AreEqual("ARM64", Architecture.ARM64.ToString(),
            "Architecture.ARM64 should serialize as 'ARM64'.");
    }

    #endregion
}
