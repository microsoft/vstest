// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json.Linq;

    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

    [TestClass]
    public class TestResultSerializationTests
    {
        private static TestCase testCase = new TestCase(
            "sampleTestClass.sampleTestCase",
            new Uri("executor://sampleTestExecutor"),
            "sampleTest.dll");

        private static DateTimeOffset startTime = new DateTimeOffset(new DateTime(2007, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        private static TestResult testResult = new TestResult(testCase)
                                                   {
                                                       // Attachments = ?
                                                       // Messages = ?
                                                       Outcome = TestOutcome.Passed,
                                                       ErrorMessage = "sampleError",
                                                       ErrorStackTrace = "sampleStackTrace",
                                                       DisplayName = "sampleTestResult",
                                                       ComputerName = "sampleComputerName",
                                                       Duration = TimeSpan.MaxValue,
                                                       StartTime = startTime,
                                                       EndTime = DateTimeOffset.MaxValue
                                                   };

        #region v1 tests

        [TestMethod]
        public void TestResultJsonShouldContainAllPropertiesOnSerialization()
        {
            var json = Serialize(testResult);

            // Use raw deserialization to validate basic properties
            dynamic data = JObject.Parse(json);
            dynamic properties = data["Properties"];
            Assert.AreEqual("TestResult.Outcome", properties[0]["Key"]["Id"].Value);
            Assert.AreEqual(1, properties[0]["Value"].Value);
            Assert.AreEqual("TestResult.ErrorMessage", properties[1]["Key"]["Id"].Value);
            Assert.AreEqual("sampleError", properties[1]["Value"].Value);
            Assert.AreEqual("TestResult.ErrorStackTrace", properties[2]["Key"]["Id"].Value);
            Assert.AreEqual("sampleStackTrace", properties[2]["Value"].Value);
            Assert.AreEqual("TestResult.DisplayName", properties[3]["Key"]["Id"].Value);
            Assert.AreEqual("sampleTestResult", properties[3]["Value"].Value);
            Assert.AreEqual("TestResult.ComputerName", properties[4]["Key"]["Id"].Value);
            Assert.AreEqual("sampleComputerName", properties[4]["Value"].Value);
            Assert.AreEqual("TestResult.Duration", properties[5]["Key"]["Id"].Value);
            Assert.AreEqual("10675199.02:48:05.4775807", properties[5]["Value"].Value);

            // By default json.net converts DateTimes to current time zone
            Assert.AreEqual("TestResult.StartTime", properties[6]["Key"]["Id"].Value);
            Assert.AreEqual(startTime.Year, ((DateTimeOffset)properties[6]["Value"].Value).Year);
            Assert.AreEqual("TestResult.EndTime", properties[7]["Key"]["Id"].Value);
            Assert.AreEqual(DateTimeOffset.MaxValue.Year, ((DateTimeOffset)properties[7]["Value"].Value).Year);
        }

        [TestMethod]
        public void TestResultObjectShouldContainAllPropertiesOnDeserialization()
        {
            var json = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"}]},\"Attachments\":[],\"Messages\":[],\"Properties\":[{\"Key\":{\"Id\":\"TestResult.Outcome\",\"Label\":\"Outcome\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome\"},\"Value\":1},{\"Key\":{\"Id\":\"TestResult.ErrorMessage\",\"Label\":\"Error Message\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleError\"},{\"Key\":{\"Id\":\"TestResult.ErrorStackTrace\",\"Label\":\"Error Stack Trace\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleStackTrace\"},{\"Key\":{\"Id\":\"TestResult.DisplayName\",\"Label\":\"TestResult Display Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestResult\"},{\"Key\":{\"Id\":\"TestResult.ComputerName\",\"Label\":\"Computer Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleComputerName\"},{\"Key\":{\"Id\":\"TestResult.Duration\",\"Label\":\"Duration\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.TimeSpan\"},\"Value\":\"10675199.02:48:05.4775807\"},{\"Key\":{\"Id\":\"TestResult.StartTime\",\"Label\":\"Start Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"2007-03-10T00:00:00+00:00\"},{\"Key\":{\"Id\":\"TestResult.EndTime\",\"Label\":\"End Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"9999-12-31T23:59:59.9999999+00:00\"}]}";

            var test = Deserialize<TestResult>(json);

            Assert.AreEqual(testResult.TestCase.Id, test.TestCase.Id);
            Assert.AreEqual(testResult.Attachments.Count, test.Attachments.Count);
            Assert.AreEqual(testResult.Messages.Count, test.Messages.Count);

            Assert.AreEqual(testResult.ComputerName, test.ComputerName);
            Assert.AreEqual(testResult.DisplayName, test.DisplayName);
            Assert.AreEqual(testResult.Duration, test.Duration);
            Assert.AreEqual(testResult.EndTime, test.EndTime);
            Assert.AreEqual(testResult.ErrorMessage, test.ErrorMessage);
            Assert.AreEqual(testResult.ErrorStackTrace, test.ErrorStackTrace);
            Assert.AreEqual(testResult.Outcome, test.Outcome);
            Assert.AreEqual(testResult.StartTime, test.StartTime);
        }

        [TestMethod]
        public void TestResultObjectShouldSerializeAttachments()
        {
            var result = new TestResult(testCase);
            result.StartTime = default(DateTimeOffset);
            result.EndTime = default(DateTimeOffset);
            result.Attachments.Add(new AttachmentSet(new Uri("http://dummyUri"), "sampleAttachment"));
            var expectedJson = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"},{\"Key\":{\"Id\":\"TestCase.CodeFilePath\",\"Label\":\"File Path\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestCase.DisplayName\",\"Label\":\"Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.Id\",\"Label\":\"Id\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Guid\"},\"Value\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\"},{\"Key\":{\"Id\":\"TestCase.LineNumber\",\"Label\":\"Line Number\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Int32\"},\"Value\":-1}]},\"Attachments\":[{\"Uri\":\"http://dummyUri\",\"DisplayName\":\"sampleAttachment\",\"Attachments\":[]}],\"Messages\":[],\"Properties\":[{\"Key\":{\"Id\":\"TestResult.Outcome\",\"Label\":\"Outcome\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome, Microsoft.VisualStudio.TestPlatform.ObjectModel, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"},\"Value\":0},{\"Key\":{\"Id\":\"TestResult.ErrorMessage\",\"Label\":\"Error Message\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ErrorStackTrace\",\"Label\":\"Error Stack Trace\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.DisplayName\",\"Label\":\"TestResult Display Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ComputerName\",\"Label\":\"Computer Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"\"},{\"Key\":{\"Id\":\"TestResult.Duration\",\"Label\":\"Duration\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.TimeSpan\"},\"Value\":\"00:00:00\"},{\"Key\":{\"Id\":\"TestResult.StartTime\",\"Label\":\"Start Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"},{\"Key\":{\"Id\":\"TestResult.EndTime\",\"Label\":\"End Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"}]}";

            var json = Serialize(result);

            Assert.AreEqual(expectedJson, json);
        }

        [TestMethod]
        public void TestResultObjectShouldDeserializeAttachments()
        {
            var json = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"}]},\"Attachments\":[{\"Uri\":\"http://dummyUri\",\"DisplayName\":\"sampleAttachment\",\"Attachments\":[]}],\"Messages\":[],\"Properties\":[]}";

            var result = Deserialize<TestResult>(json);

            Assert.AreEqual(1, result.Attachments.Count);
            Assert.AreEqual(new Uri("http://dummyUri"), result.Attachments[0].Uri);
            Assert.AreEqual("sampleAttachment", result.Attachments[0].DisplayName);
        }

        [TestMethod]
        public void TestResultObjectShouldSerializeDefaultValues()
        {
            var result = new TestResult(testCase);
            result.StartTime = default(DateTimeOffset);
            result.EndTime = default(DateTimeOffset);
            var expectedJson = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"},{\"Key\":{\"Id\":\"TestCase.CodeFilePath\",\"Label\":\"File Path\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestCase.DisplayName\",\"Label\":\"Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.Id\",\"Label\":\"Id\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Guid\"},\"Value\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\"},{\"Key\":{\"Id\":\"TestCase.LineNumber\",\"Label\":\"Line Number\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Int32\"},\"Value\":-1}]},\"Attachments\":[],\"Messages\":[],\"Properties\":[{\"Key\":{\"Id\":\"TestResult.Outcome\",\"Label\":\"Outcome\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome, Microsoft.VisualStudio.TestPlatform.ObjectModel, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"},\"Value\":0},{\"Key\":{\"Id\":\"TestResult.ErrorMessage\",\"Label\":\"Error Message\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ErrorStackTrace\",\"Label\":\"Error Stack Trace\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.DisplayName\",\"Label\":\"TestResult Display Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ComputerName\",\"Label\":\"Computer Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"\"},{\"Key\":{\"Id\":\"TestResult.Duration\",\"Label\":\"Duration\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.TimeSpan\"},\"Value\":\"00:00:00\"},{\"Key\":{\"Id\":\"TestResult.StartTime\",\"Label\":\"Start Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"},{\"Key\":{\"Id\":\"TestResult.EndTime\",\"Label\":\"End Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"}]}";

            var json = Serialize(result);

            // Values that should be null: DisplayName, ErrorMessage, ErrorStackTrace
            // Values that should be empty: ComputerName
            Assert.AreEqual(expectedJson, json);
        }

        [TestMethod]
        public void TestResultObjectShouldDeserializeDefaultValues()
        {
            var json = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"},{\"Key\":{\"Id\":\"TestCase.CodeFilePath\",\"Label\":\"File Path\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestCase.DisplayName\",\"Label\":\"Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.Id\",\"Label\":\"Id\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Guid\"},\"Value\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\"},{\"Key\":{\"Id\":\"TestCase.LineNumber\",\"Label\":\"Line Number\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Int32\"},\"Value\":-1}]},\"Attachments\":[],\"Messages\":[],\"Properties\":[{\"Key\":{\"Id\":\"TestResult.Outcome\",\"Label\":\"Outcome\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome, Microsoft.VisualStudio.TestPlatform.ObjectModel, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"},\"Value\":0},{\"Key\":{\"Id\":\"TestResult.ErrorMessage\",\"Label\":\"Error Message\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ErrorStackTrace\",\"Label\":\"Error Stack Trace\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.DisplayName\",\"Label\":\"TestResult Display Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ComputerName\",\"Label\":\"Computer Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"\"},{\"Key\":{\"Id\":\"TestResult.Duration\",\"Label\":\"Duration\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.TimeSpan\"},\"Value\":\"00:00:00\"},{\"Key\":{\"Id\":\"TestResult.StartTime\",\"Label\":\"Start Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"},{\"Key\":{\"Id\":\"TestResult.EndTime\",\"Label\":\"End Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"}]}";

            var result = Deserialize<TestResult>(json);

            Assert.AreEqual(0, result.Attachments.Count);
            Assert.AreEqual(0, result.Messages.Count);
            Assert.IsNull(result.DisplayName);
            Assert.IsNull(result.ErrorMessage);
            Assert.IsNull(result.ErrorStackTrace);
            Assert.AreEqual(string.Empty, result.ComputerName);
        }

        [TestMethod]
        public void TestResultPropertiesShouldGetRegisteredAsPartOfDeserialization()
        {
            TestProperty.TryUnregister("DummyProperty", out var property);
            var json = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"}," +
                "{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"}," +
                "{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"}," +
                "{\"Key\":{\"Id\":\"TestCase.CodeFilePath\",\"Label\":\"File Path\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null}," +
                "{\"Key\":{\"Id\":\"TestCase.DisplayName\",\"Label\":\"Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"}," +
                "{\"Key\":{\"Id\":\"TestCase.Id\",\"Label\":\"Id\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Guid\"},\"Value\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\"}," +
                "{\"Key\":{\"Id\":\"TestCase.LineNumber\",\"Label\":\"Line Number\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Int32\"},\"Value\":-1}]}," +
                "\"Attachments\":[],\"Messages\":[]," +
                "\"Properties\":[{\"Key\":{\"Id\":\"TestResult.Outcome\",\"Label\":\"Outcome\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome, Microsoft.VisualStudio.TestPlatform.ObjectModel, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"},\"Value\":0}," +
                "{\"Key\":{\"Id\":\"TestResult.ErrorMessage\",\"Label\":\"Error Message\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ErrorStackTrace\",\"Label\":\"Error Stack Trace\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":null}," +
                "{\"Key\":{\"Id\":\"TestResult.DisplayName\",\"Label\":\"TestResult Display Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":null},{\"Key\":{\"Id\":\"TestResult.ComputerName\",\"Label\":\"Computer Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"\"}," +
                "{\"Key\":{\"Id\":\"TestResult.Duration\",\"Label\":\"Duration\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.TimeSpan\"},\"Value\":\"00:00:00\"},{\"Key\":{\"Id\":\"TestResult.StartTime\",\"Label\":\"Start Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"}," +
                "{\"Key\":{\"Id\":\"DummyProperty\",\"Label\":\"DummyPropertyLabel\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":5,\"ValueType\":\"System.String\"},\"Value\":\"dummyString\"}," +
                "{\"Key\":{\"Id\":\"TestResult.EndTime\",\"Label\":\"End Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"}]}";
            var test = Deserialize<TestResult>(json);

            this.VerifyDummyPropertyIsRegistered();
        }

        #endregion

        #region v2 Tests

        [TestMethod]
        public void TestResultJsonShouldContainAllPropertiesOnSerializationV2()
        {
            var json = Serialize(testResult, 2);

            // Use raw deserialization to validate basic properties
            dynamic data = JObject.Parse(json);

            Assert.AreEqual(1, data["Outcome"].Value);
            Assert.AreEqual("sampleError", data["ErrorMessage"].Value);
            Assert.AreEqual("sampleStackTrace", data["ErrorStackTrace"].Value);
            Assert.AreEqual("sampleTestResult", data["DisplayName"].Value);
            Assert.AreEqual("sampleComputerName", data["ComputerName"].Value);
            Assert.AreEqual("10675199.02:48:05.4775807", data["Duration"].Value);

            // By default json.net converts DateTimes to current time zone
            Assert.AreEqual(startTime.Year, ((DateTimeOffset)data["StartTime"].Value).Year);
            Assert.AreEqual(DateTimeOffset.MaxValue.Year, ((DateTimeOffset)data["EndTime"].Value).Year);
        }

        [TestMethod]
        public void TestResultObjectShouldContainAllPropertiesOnDeserializationV2()
        {
            var json = "{\"TestCase\":{\"Id\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\",\"FullyQualifiedName\":\"sampleTestClass.sampleTestCase\",\"DisplayName\":\"sampleTestClass.sampleTestCase\",\"ExecutorUri\":\"executor://sampleTestExecutor\",\"Source\":\"sampleTest.dll\",\"CodeFilePath\":null,\"LineNumber\":-1,\"Properties\":[]},\"Attachments\":[],\"Outcome\":1,\"ErrorMessage\":\"sampleError\",\"ErrorStackTrace\":\"sampleStackTrace\",\"DisplayName\":\"sampleTestResult\",\"Messages\":[],\"ComputerName\":\"sampleComputerName\",\"Duration\":\"10675199.02:48:05.4775807\",\"StartTime\":\"2007-03-10T00:00:00+00:00\",\"EndTime\":\"9999-12-31T23:59:59.9999999+00:00\",\"Properties\":[]}";

            var test = Deserialize<TestResult>(json, 2);

            Assert.AreEqual(testResult.TestCase.Id, test.TestCase.Id);
            Assert.AreEqual(testResult.Attachments.Count, test.Attachments.Count);
            Assert.AreEqual(testResult.Messages.Count, test.Messages.Count);

            Assert.AreEqual(testResult.ComputerName, test.ComputerName);
            Assert.AreEqual(testResult.DisplayName, test.DisplayName);
            Assert.AreEqual(testResult.Duration, test.Duration);
            Assert.AreEqual(testResult.EndTime, test.EndTime);
            Assert.AreEqual(testResult.ErrorMessage, test.ErrorMessage);
            Assert.AreEqual(testResult.ErrorStackTrace, test.ErrorStackTrace);
            Assert.AreEqual(testResult.Outcome, test.Outcome);
            Assert.AreEqual(testResult.StartTime, test.StartTime);
        }

        [TestMethod]
        public void TestResultObjectShouldSerializeAttachmentsV2()
        {
            var result = new TestResult(testCase);
            result.StartTime = default(DateTimeOffset);
            result.EndTime = default(DateTimeOffset);
            result.Attachments.Add(new AttachmentSet(new Uri("http://dummyUri"), "sampleAttachment"));
            var expectedJson = "{\"TestCase\":{\"Id\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\",\"FullyQualifiedName\":\"sampleTestClass.sampleTestCase\",\"DisplayName\":\"sampleTestClass.sampleTestCase\",\"ExecutorUri\":\"executor://sampleTestExecutor\",\"Source\":\"sampleTest.dll\",\"CodeFilePath\":null,\"LineNumber\":-1,\"Properties\":[]},\"Attachments\":[{\"Uri\":\"http://dummyUri\",\"DisplayName\":\"sampleAttachment\",\"Attachments\":[]}],\"Outcome\":0,\"ErrorMessage\":null,\"ErrorStackTrace\":null,\"DisplayName\":null,\"Messages\":[],\"Timers\":[],\"ComputerName\":null,\"Duration\":\"00:00:00\",\"StartTime\":\"0001-01-01T00:00:00+00:00\",\"EndTime\":\"0001-01-01T00:00:00+00:00\",\"Properties\":[]}";

            var json = Serialize(result, 2);

            Assert.AreEqual(expectedJson, json);
        }

        [TestMethod]
        public void TestResultObjectShouldDeserializeAttachmentsV2()
        {
            var json = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"},{\"Key\":{\"Id\":\"TestCase.Id\",\"Label\":\"Id\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Guid\"},\"Value\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\"}]},\"Attachments\":[{\"Uri\":\"http://dummyUri\",\"DisplayName\":\"sampleAttachment\",\"Attachments\":[]}],\"Messages\":[],\"Properties\":[]}";

            var result = Deserialize<TestResult>(json, 2);

            Assert.AreEqual(1, result.Attachments.Count);
            Assert.AreEqual(new Uri("http://dummyUri"), result.Attachments[0].Uri);
            Assert.AreEqual("sampleAttachment", result.Attachments[0].DisplayName);
        }

        [TestMethod]
        public void TestResultPropertiesShouldGetRegisteredAsPartOfDeserializationV2()
        {
            TestProperty.TryUnregister("DummyProperty", out var property);
            var json = "{\"TestCase\":{\"Id\":\"28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b\",\"FullyQualifiedName\":\"sampleTestClass.sampleTestCase\",\"DisplayName\":\"sampleTestClass.sampleTestCase\",\"ExecutorUri\":\"executor://sampleTestExecutor\",\"Source\":\"sampleTest.dll\",\"CodeFilePath\":null,\"LineNumber\":-1,\"Properties\":[]},\"Attachments\":[],\"Outcome\":1,\"ErrorMessage\":\"sampleError\",\"ErrorStackTrace\":\"sampleStackTrace\",\"DisplayName\":\"sampleTestResult\",\"Messages\":[],\"ComputerName\":\"sampleComputerName\",\"Duration\":\"10675199.02:48:05.4775807\",\"StartTime\":\"2007-03-10T00:00:00+00:00\",\"EndTime\":\"9999-12-31T23:59:59.9999999+00:00\"," +
                "\"Properties\":[{\"Key\":{\"Id\":\"DummyProperty\",\"Label\":\"DummyPropertyLabel\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":5,\"ValueType\":\"System.String\"},\"Value\":\"dummyString\"},]}";

            var test = Deserialize<TestResult>(json, 2);

            this.VerifyDummyPropertyIsRegistered();
        }

        #endregion

        private static string Serialize<T>(T data, int version = 1)
        {
            return JsonDataSerializer.Instance.Serialize(data, version);
        }

        private static T Deserialize<T>(string json, int version = 1)
        {
            return JsonDataSerializer.Instance.Deserialize<T>(json, version);
        }

        private void VerifyDummyPropertyIsRegistered()
        {
            var dummyProperty = TestProperty.Find("DummyProperty");
            Assert.IsNotNull(dummyProperty);
            Assert.AreEqual("DummyPropertyLabel", dummyProperty.Label);
            Assert.AreEqual("System.String", dummyProperty.ValueType);
            Assert.AreEqual(5, (int)dummyProperty.Attributes);
        }
    }
}