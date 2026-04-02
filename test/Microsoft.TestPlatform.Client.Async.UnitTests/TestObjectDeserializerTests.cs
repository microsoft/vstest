// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

using Microsoft.TestPlatform.Client.Async.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Client.Async.UnitTests;

[TestClass]
public class TestObjectDeserializerTests
{
    [TestMethod]
    public void DeserializeTestCase_BasicProperties()
    {
        string json = """
        {
          "Properties": [
            {
              "Key": { "Id": "TestCase.FullyQualifiedName", "Label": "FullyQualifiedName", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.String" },
              "Value": "MyNamespace.MyClass.TestMethod1"
            },
            {
              "Key": { "Id": "TestCase.ExecutorUri", "Label": "Executor Uri", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.Uri" },
              "Value": "executor://mstest/v2"
            },
            {
              "Key": { "Id": "TestCase.Source", "Label": "Source", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
              "Value": "C:\\tests\\MyTests.dll"
            },
            {
              "Key": { "Id": "TestCase.DisplayName", "Label": "Name", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
              "Value": "TestMethod1"
            },
            {
              "Key": { "Id": "TestCase.Id", "Label": "Id", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.Guid" },
              "Value": "11111111-1111-1111-1111-111111111111"
            }
          ]
        }
        """;

        var element = JsonDocument.Parse(json).RootElement;
        var testCase = TestObjectDeserializer.DeserializeTestCase(element);

        Assert.AreEqual("MyNamespace.MyClass.TestMethod1", testCase.FullyQualifiedName);
        Assert.AreEqual("executor://mstest/v2", testCase.ExecutorUri.OriginalString);
        Assert.AreEqual("C:\\tests\\MyTests.dll", testCase.Source);
        Assert.AreEqual("TestMethod1", testCase.DisplayName);
        Assert.AreEqual(new Guid("11111111-1111-1111-1111-111111111111"), testCase.Id);
    }

    [TestMethod]
    public void DeserializeTestCase_WithCodeFileAndLineNumber()
    {
        string json = """
        {
          "Properties": [
            {
              "Key": { "Id": "TestCase.FullyQualifiedName", "Label": "FullyQualifiedName", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.String" },
              "Value": "MyNamespace.MyClass.TestMethod2"
            },
            {
              "Key": { "Id": "TestCase.ExecutorUri", "Label": "Executor Uri", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.Uri" },
              "Value": "executor://nunit"
            },
            {
              "Key": { "Id": "TestCase.Source", "Label": "Source", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
              "Value": "tests.dll"
            },
            {
              "Key": { "Id": "TestCase.CodeFilePath", "Label": "File Path", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
              "Value": "C:\\src\\Tests.cs"
            },
            {
              "Key": { "Id": "TestCase.LineNumber", "Label": "Line Number", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.Int32" },
              "Value": 42
            }
          ]
        }
        """;

        var element = JsonDocument.Parse(json).RootElement;
        var testCase = TestObjectDeserializer.DeserializeTestCase(element);

        Assert.AreEqual("C:\\src\\Tests.cs", testCase.CodeFilePath);
        Assert.AreEqual(42, testCase.LineNumber);
    }

    [TestMethod]
    public void DeserializeTestCase_EmptyProperties_ReturnsDefaultTestCase()
    {
        string json = """{ "Properties": [] }""";

        var element = JsonDocument.Parse(json).RootElement;
        var testCase = TestObjectDeserializer.DeserializeTestCase(element);

        // With no properties, the deserializer creates a TestCase with empty defaults.
        Assert.IsNotNull(testCase);
    }

    [TestMethod]
    public void DeserializeTestResult_BasicProperties()
    {
        string json = """
        {
          "TestCase": {
            "Properties": [
              {
                "Key": { "Id": "TestCase.FullyQualifiedName", "Label": "FullyQualifiedName", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.String" },
                "Value": "MyTest"
              },
              {
                "Key": { "Id": "TestCase.ExecutorUri", "Label": "Executor Uri", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.Uri" },
                "Value": "executor://mstest/v2"
              },
              {
                "Key": { "Id": "TestCase.Source", "Label": "Source", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
                "Value": "test.dll"
              }
            ]
          },
          "Properties": [
            {
              "Key": { "Id": "TestResult.Outcome", "Label": "Outcome", "Category": "", "Description": "", "Attributes": 0, "ValueType": "Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome" },
              "Value": 2
            },
            {
              "Key": { "Id": "TestResult.ErrorMessage", "Label": "Error Message", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
              "Value": "Assert.AreEqual failed."
            },
            {
              "Key": { "Id": "TestResult.Duration", "Label": "Duration", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.TimeSpan" },
              "Value": "00:00:01.500"
            }
          ]
        }
        """;

        var element = JsonDocument.Parse(json).RootElement;
        var result = TestObjectDeserializer.DeserializeTestResult(element);

        Assert.AreEqual("MyTest", result.TestCase.FullyQualifiedName);
        Assert.AreEqual(TestOutcome.Failed, result.Outcome);
        Assert.AreEqual("Assert.AreEqual failed.", result.ErrorMessage);
        Assert.AreEqual(TimeSpan.FromSeconds(1.5), result.Duration);
    }

    [TestMethod]
    public void DeserializeTestResult_PassedTest()
    {
        string json = """
        {
          "TestCase": {
            "Properties": [
              {
                "Key": { "Id": "TestCase.FullyQualifiedName", "Label": "FullyQualifiedName", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.String" },
                "Value": "PassingTest"
              },
              {
                "Key": { "Id": "TestCase.ExecutorUri", "Label": "Executor Uri", "Category": "", "Description": "", "Attributes": 1, "ValueType": "System.Uri" },
                "Value": "executor://mstest/v2"
              },
              {
                "Key": { "Id": "TestCase.Source", "Label": "Source", "Category": "", "Description": "", "Attributes": 0, "ValueType": "System.String" },
                "Value": "test.dll"
              }
            ]
          },
          "Properties": [
            {
              "Key": { "Id": "TestResult.Outcome", "Label": "Outcome", "Category": "", "Description": "", "Attributes": 0, "ValueType": "Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome" },
              "Value": 1
            }
          ]
        }
        """;

        var element = JsonDocument.Parse(json).RootElement;
        var result = TestObjectDeserializer.DeserializeTestResult(element);

        Assert.AreEqual(TestOutcome.Passed, result.Outcome);
    }

    [TestMethod]
    public void TestCaseDto_FromTestCase_RoundTrips()
    {
        var original = new TestCase("Ns.Class.Method", new Uri("executor://test"), "test.dll")
        {
            DisplayName = "My Test",
            CodeFilePath = "test.cs",
            LineNumber = 10,
        };

        var dto = TestCaseDto.FromTestCase(original);

        // Serialize and deserialize.
        string json = JsonSerializer.Serialize(dto);
        var element = JsonDocument.Parse(json).RootElement;
        var deserialized = TestObjectDeserializer.DeserializeTestCase(element);

        Assert.AreEqual(original.FullyQualifiedName, deserialized.FullyQualifiedName);
        Assert.AreEqual(original.ExecutorUri.OriginalString, deserialized.ExecutorUri.OriginalString);
        Assert.AreEqual(original.Source, deserialized.Source);
        Assert.AreEqual(original.DisplayName, deserialized.DisplayName);
        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.AreEqual(original.CodeFilePath, deserialized.CodeFilePath);
        Assert.AreEqual(original.LineNumber, deserialized.LineNumber);
    }
}
