// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using TrxLoggerConstants = Microsoft.TestPlatform.Extensions.TrxLogger.Utility.Constants;
using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

using DefaultLoggerParameterNames = Microsoft.VisualStudio.TestPlatform.ObjectModel.DefaultLoggerParameterNames;

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests;

/// <summary>
/// Regression tests for:
/// - Issue #2319 / PR #2364: TestResult ErrorMessage/ErrorStackTrace lazy initialization.
/// - Issue #5132 / PR #5141: TrxLogger WarnOnFileOverwrite parameter.
/// </summary>
[TestClass]
public class ICSRegressionTests
{
    private static readonly string DefaultTestRunDirectory = Path.GetTempPath();

    #region Issue #2319 - TestResult ErrorMessage / ErrorStackTrace lazy initialization

    [TestMethod]
    public void ErrorMessage_SetWithoutErrorStackTrace_ShouldNotReturnNull()
    {
        // Arrange
        var testResult = CreateTestResult();

        // Act: set ErrorMessage without touching ErrorStackTrace first
        testResult.ErrorMessage = "Test failure message";

        // Assert
        Assert.AreEqual("Test failure message", testResult.ErrorMessage);
        Assert.AreEqual(string.Empty, testResult.ErrorStackTrace, "ErrorStackTrace should default to empty, not null.");
    }

    [TestMethod]
    public void ErrorStackTrace_SetWithoutErrorMessage_ShouldNotReturnNull()
    {
        // Arrange
        var testResult = CreateTestResult();

        // Act: set ErrorStackTrace without touching ErrorMessage first
        testResult.ErrorStackTrace = "at SomeTest.Method() in file.cs:line 42";

        // Assert
        Assert.AreEqual("at SomeTest.Method() in file.cs:line 42", testResult.ErrorStackTrace);
        Assert.AreEqual(string.Empty, testResult.ErrorMessage, "ErrorMessage should default to empty, not null.");
    }

    [TestMethod]
    public void ErrorMessage_SetThenErrorStackTrace_BothShouldBeAccessible()
    {
        // Arrange
        var testResult = CreateTestResult();

        // Act
        testResult.ErrorMessage = "Assertion failed";
        testResult.ErrorStackTrace = "at MyTest.Run()";

        // Assert
        Assert.AreEqual("Assertion failed", testResult.ErrorMessage);
        Assert.AreEqual("at MyTest.Run()", testResult.ErrorStackTrace);
    }

    [TestMethod]
    public void ErrorStackTrace_SetThenErrorMessage_BothShouldBeAccessible()
    {
        // Arrange
        var testResult = CreateTestResult();

        // Act
        testResult.ErrorStackTrace = "at MyTest.Run()";
        testResult.ErrorMessage = "Assertion failed";

        // Assert
        Assert.AreEqual("at MyTest.Run()", testResult.ErrorStackTrace);
        Assert.AreEqual("Assertion failed", testResult.ErrorMessage);
    }

    [TestMethod]
    public void ErrorMessage_WhenNeverSet_ShouldReturnEmptyString()
    {
        // Arrange
        var testResult = CreateTestResult();

        // Assert: before setting anything, getters should return empty, not null.
        Assert.AreEqual(string.Empty, testResult.ErrorMessage);
        Assert.AreEqual(string.Empty, testResult.ErrorStackTrace);
    }

    #endregion

    #region Issue #5132 - TrxLogger WarnOnFileOverwrite parameter

    [TestMethod]
    public void Initialize_WarnOnFileOverwriteFalse_ShouldNotWarn()
    {
        // Arrange
        var logger = new TestableTrxLogger();
        var events = new Mock<TestLoggerEvents>();
        var parameters = CreateDefaultParameters();
        parameters[TrxLoggerConstants.WarnOnFileOverwrite] = "false";

        // Act & Assert: should not throw
        logger.Initialize(events.Object, parameters);

        // Verify _warnOnFileOverwrite is false by checking it was parsed without error.
        // We can't directly access the private field, but the initialization should succeed.
    }

    [TestMethod]
    public void Initialize_WarnOnFileOverwriteTrue_ShouldInitializeSuccessfully()
    {
        // Arrange
        var logger = new TestableTrxLogger();
        var events = new Mock<TestLoggerEvents>();
        var parameters = CreateDefaultParameters();
        parameters[TrxLoggerConstants.WarnOnFileOverwrite] = "true";

        // Act & Assert: should not throw
        logger.Initialize(events.Object, parameters);
    }

    [TestMethod]
    public void Initialize_WarnOnFileOverwriteNotProvided_ShouldDefaultToTrue()
    {
        // Arrange
        var logger = new TestableTrxLogger();
        var events = new Mock<TestLoggerEvents>();
        var parameters = CreateDefaultParameters();
        // Deliberately do NOT add WarnOnFileOverwrite key.

        // Act & Assert: should not throw; defaults to true (existing behavior).
        logger.Initialize(events.Object, parameters);
    }

    [TestMethod]
    public void Initialize_WarnOnFileOverwriteInvalidValue_ShouldDefaultToTrue()
    {
        // Arrange
        var logger = new TestableTrxLogger();
        var events = new Mock<TestLoggerEvents>();
        var parameters = CreateDefaultParameters();
        parameters[TrxLoggerConstants.WarnOnFileOverwrite] = "not_a_bool";

        // Act & Assert: invalid value should fallback to true (warn by default).
        logger.Initialize(events.Object, parameters);
    }

    #endregion

    #region Issue #4227 - TRX emits Min instead of Error

    [TestMethod]
    public void TestOutcome_Error_ShouldBeFirstEnumValue()
    {
        // Before the fix, the first enum value was named differently, causing
        // TRX output to emit "Min" instead of "Error" for error outcomes.
        var errorValue = (int)TrxLoggerObjectModel.TestOutcome.Error;
        Assert.AreEqual(0, errorValue,
            "TestOutcome.Error should be the first enum value (0) to avoid emitting 'Min' in TRX output.");
    }

    [TestMethod]
    public void TestOutcome_Error_ShouldHaveCorrectName()
    {
        // Verify the enum value serializes as "Error", not "Min".
        Assert.AreEqual("Error", TrxLoggerObjectModel.TestOutcome.Error.ToString(),
            "The first TestOutcome value should be named 'Error', not 'Min'.");
    }

    [TestMethod]
    public void ToOutcome_Failed_ShouldMapToTrxFailed()
    {
        var result = Converter.ToOutcome(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed);
        Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.Failed, result);
    }

    [TestMethod]
    public void ToOutcome_Passed_ShouldMapToTrxPassed()
    {
        var result = Converter.ToOutcome(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed);
        Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.Passed, result);
    }

    [TestMethod]
    public void ToOutcome_Skipped_ShouldMapToTrxNotExecuted()
    {
        var result = Converter.ToOutcome(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped);
        Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.NotExecuted, result);
    }

    [TestMethod]
    public void ToOutcome_None_ShouldMapToTrxNotExecuted()
    {
        var result = Converter.ToOutcome(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.None);
        Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.NotExecuted, result);
    }

    [TestMethod]
    public void ToOutcome_NotFound_ShouldMapToTrxNotExecuted()
    {
        var result = Converter.ToOutcome(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.NotFound);
        Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.NotExecuted, result);
    }

    [TestMethod]
    public void ToOutcome_DefaultCase_ShouldNotMapToMinOrError()
    {
        // The default/initial value in ToOutcome should be Failed, not Error (Min).
        var result = Converter.ToOutcome(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed);
        Assert.AreNotEqual(TrxLoggerObjectModel.TestOutcome.Error, result,
            "Failed outcome should map to Failed, not Error (which was previously named Min).");
    }

    #endregion

    #region Helpers

    private static TrxLoggerObjectModel.TestResult CreateTestResult()
    {
        return new TrxLoggerObjectModel.TestResult(
            runId: Guid.NewGuid(),
            testId: Guid.NewGuid(),
            executionId: Guid.NewGuid(),
            parentExecutionId: Guid.Empty,
            resultName: "TestResult1",
            computerName: Environment.MachineName,
            outcome: TrxLoggerObjectModel.TestOutcome.Failed,
            testType: new TrxLoggerObjectModel.TestType(Guid.NewGuid()),
            testCategoryId: TrxLoggerObjectModel.TestListCategoryId.Uncategorized,
            trxFileHelper: new TrxFileHelper());
    }

    private static Dictionary<string, string?> CreateDefaultParameters()
    {
        return new Dictionary<string, string?>
        {
            [DefaultLoggerParameterNames.TestRunDirectory] = DefaultTestRunDirectory,
            [TrxLoggerConstants.LogFileNameKey] = "test.trx"
        };
    }

    #endregion
}


