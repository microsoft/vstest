// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests;

[TestClass]
public class HtmlTransformerTests
{
    [TestMethod]
    public void TransformShouldHandleSpecialCharactersWithoutThrowingException()
    {
        // Create a simple XML with special characters that could cause issues
        var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun>
    <Results>
        <UnitTestResult testName=""TestWithSpecialChars"" outcome=""Failed"">
            <Output>
                <ErrorInfo>
                    <Message>Test failed with special chars: &#x1;&#x2;&#xFFFF;</Message>
                    <StackTrace>Stack trace with special chars: &#x1;&#x2;</StackTrace>
                </ErrorInfo>
            </Output>
        </UnitTestResult>
    </Results>
</TestRun>";

        var xmlFile = Path.GetTempFileName();
        var htmlFile = Path.ChangeExtension(xmlFile, ".html");

        try
        {
            File.WriteAllText(xmlFile, xmlContent, Encoding.UTF8);
            
            var transformer = new HtmlTransformer();
            
            // This should not throw an exception, even with special characters
            try
            {
                transformer.Transform(xmlFile, htmlFile);
            }
            catch (Exception ex)
            {
                Assert.Fail($"HtmlTransformer.Transform should not throw an exception with special characters: {ex.Message}");
            }
            
            // Verify that HTML file was created
            Assert.IsTrue(File.Exists(htmlFile), "HTML file should be created");
            
            // Verify that HTML file has content
            var htmlContent = File.ReadAllText(htmlFile);
            Assert.IsFalse(string.IsNullOrEmpty(htmlContent), "HTML content should not be empty");
        }
        finally
        {
            if (File.Exists(xmlFile)) File.Delete(xmlFile);
            if (File.Exists(htmlFile)) File.Delete(htmlFile);
        }
    }

    [TestMethod]
    public void TransformShouldCreateValidHtmlOutput()
    {
        // Create a basic XML structure
        var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun>
    <Results>
        <UnitTestResult testName=""SimpleTest"" outcome=""Passed"">
            <Output>
                <StdOut>Test output</StdOut>
            </Output>
        </UnitTestResult>
    </Results>
</TestRun>";

        var xmlFile = Path.GetTempFileName();
        var htmlFile = Path.ChangeExtension(xmlFile, ".html");

        try
        {
            File.WriteAllText(xmlFile, xmlContent, Encoding.UTF8);
            
            var transformer = new HtmlTransformer();
            transformer.Transform(xmlFile, htmlFile);
            
            // Verify that HTML file was created and has content
            Assert.IsTrue(File.Exists(htmlFile), "HTML file should be created");
            var htmlContent = File.ReadAllText(htmlFile);
            Assert.IsFalse(string.IsNullOrEmpty(htmlContent), "HTML content should not be empty");
        }
        finally
        {
            if (File.Exists(xmlFile)) File.Delete(xmlFile);
            if (File.Exists(htmlFile)) File.Delete(htmlFile);
        }
    }
}