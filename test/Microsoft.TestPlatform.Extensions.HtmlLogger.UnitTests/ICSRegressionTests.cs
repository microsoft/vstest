// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.Serialization;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using HtmlLoggerImpl = Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.HtmlLogger;
using HtmlTransformerImpl = Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.HtmlTransformer;
using IHtmlTransformer = Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.IHtmlTransformer;

namespace Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests;

/// <summary>
/// Regression tests for:
/// - Issue #2318 / PR #2483: HTML logger directory creation.
/// - Issue #2414 / PR #2419: HTML logger line breaks in stack traces.
/// - Issue #2414 / PR #2419: HTML logger line breaks in stack traces.
/// </summary>
[TestClass]
public class ICSRegressionTests
{
    private readonly HtmlLoggerImpl _htmlLogger;
    private readonly Mock<IFileHelper> _mockFileHelper;
    private readonly Mock<XmlObjectSerializer> _mockXmlSerializer;
    private readonly Mock<IHtmlTransformer> _mockHtmlTransformer;

    public ICSRegressionTests()
    {
        _mockFileHelper = new Mock<IFileHelper>();
        _mockHtmlTransformer = new Mock<IHtmlTransformer>();
        _mockXmlSerializer = new Mock<XmlObjectSerializer>();
        _htmlLogger = new HtmlLoggerImpl(_mockFileHelper.Object, _mockHtmlTransformer.Object, _mockXmlSerializer.Object);
    }

    [TestMethod]
    public void Initialize_WithNonExistentDirectory_ShouldNotThrowAndSetTestResultsDirPath()
    {
        // Arrange
        var events = new Mock<TestLoggerEvents>();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            // Act: Initialize with a directory that doesn't exist yet.
            // The fix ensures Directory.CreateDirectory is called, so this should not throw.
            _htmlLogger.Initialize(events.Object, nonExistentDir);

            // Assert
            Assert.AreEqual(nonExistentDir, _htmlLogger.TestResultsDirPath);
            Assert.IsNotNull(_htmlLogger.TestRunDetails);
            Assert.IsNotNull(_htmlLogger.Results);
            Assert.IsNotNull(_htmlLogger.ResultCollectionDictionary);

            // Verify the directory was actually created
            Assert.IsTrue(Directory.Exists(nonExistentDir), "Directory.CreateDirectory should have been called during Initialize.");
        }
        finally
        {
            // Cleanup: remove the created directory
            if (Directory.Exists(nonExistentDir))
            {
                Directory.Delete(nonExistentDir);
            }
        }
    }

    [TestMethod]
    public void Initialize_WithExistingDirectory_ShouldNotThrow()
    {
        // Arrange
        var events = new Mock<TestLoggerEvents>();
        var existingDir = Path.GetTempPath();

        // Act & Assert: initializing with an existing directory should not throw.
        _htmlLogger.Initialize(events.Object, existingDir);

        Assert.AreEqual(existingDir, _htmlLogger.TestResultsDirPath);
    }

    [TestMethod]
    public void Initialize_ShouldSetAllRequiredProperties()
    {
        // Arrange
        var events = new Mock<TestLoggerEvents>();
        var testDir = Path.GetTempPath();

        // Act
        _htmlLogger.Initialize(events.Object, testDir);

        // Assert: all properties should be initialized (fix ensured no NullReferenceException later).
        Assert.IsNotNull(_htmlLogger.TestResultsDirPath, "TestResultsDirPath should be set.");
        Assert.IsNotNull(_htmlLogger.TestRunDetails, "TestRunDetails should be initialized.");
        Assert.IsNotNull(_htmlLogger.Results, "Results should be initialized.");
        Assert.IsNotNull(_htmlLogger.ResultCollectionDictionary, "ResultCollectionDictionary should be initialized.");
    }

    #region Issue #2414 - HTML logger line breaks in stack traces

    [TestMethod]
    public void HtmlTransformer_Transform_WithSpecialCharacters_ShouldNotThrow()
    {
        // The fix configured XmlReader with CheckCharacters=false and XSLT output
        // uses HTML method, allowing special characters and line breaks in stack traces.
        var xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<TestRunDetails>" +
            "<ResultSummary>" +
            "<TestResult TestName=\"Test1\">" +
            "<StackTrace>at Test.Method() in file.cs:line 42&#xD;&#xA;at Test.Method2()</StackTrace>" +
            "</TestResult>" +
            "</ResultSummary>" +
            "</TestRunDetails>";

        var xmlFile = Path.Combine(Path.GetTempPath(), $"ics_test_input_{Guid.NewGuid():N}.xml");
        var htmlFile = Path.Combine(Path.GetTempPath(), $"ics_test_output_{Guid.NewGuid():N}.html");

        try
        {
            File.WriteAllText(xmlFile, xmlContent);

            var transformer = new HtmlTransformerImpl();
            // Should not throw even with special characters and line breaks
            transformer.Transform(xmlFile, htmlFile);

            Assert.IsTrue(File.Exists(htmlFile), "Output HTML file should be created.");
            var htmlContent = File.ReadAllText(htmlFile);
            Assert.IsGreaterThan(0, htmlContent.Length, "HTML output should not be empty.");
        }
        finally
        {
            if (File.Exists(xmlFile)) File.Delete(xmlFile);
            if (File.Exists(htmlFile)) File.Delete(htmlFile);
        }
    }

    [TestMethod]
    public void HtmlTransformer_Constructor_ShouldLoadXsltSuccessfully()
    {
        // Verify the HtmlTransformer can be constructed without throwing,
        // which means the embedded XSLT resource loads correctly.
        var transformer = new HtmlTransformerImpl();
        Assert.IsNotNull(transformer);
    }

    #endregion
}
