﻿// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CommunicationUtilities.UnitTests.Serialization
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
        private static TestCase testCase = new TestCase("sampleTestClass.sampleTestCase", 
                                               new Uri("executor://sampleTestExecutor"),
                                               "sampleTest.dll");

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
                                                       StartTime = DateTimeOffset.MinValue,
                                                       EndTime = DateTimeOffset.MaxValue
                                                   };

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
            Assert.AreEqual(DateTimeOffset.MinValue.Year, ((DateTimeOffset)properties[6]["Value"].Value).Year);
            Assert.AreEqual("TestResult.EndTime", properties[7]["Key"]["Id"].Value);
            Assert.AreEqual(DateTimeOffset.MaxValue.Year, ((DateTimeOffset)properties[7]["Value"].Value).Year);
        }

        [TestMethod]
        public void TestResultObjectShouldContainAllPropertiesOnDeserialization()
        {
            var json = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"}]},\"Attachments\":[],\"Messages\":[],\"Properties\":[{\"Key\":{\"Id\":\"TestResult.Outcome\",\"Label\":\"Outcome\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome\"},\"Value\":1},{\"Key\":{\"Id\":\"TestResult.ErrorMessage\",\"Label\":\"Error Message\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleError\"},{\"Key\":{\"Id\":\"TestResult.ErrorStackTrace\",\"Label\":\"Error Stack Trace\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleStackTrace\"},{\"Key\":{\"Id\":\"TestResult.DisplayName\",\"Label\":\"TestResult Display Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestResult\"},{\"Key\":{\"Id\":\"TestResult.ComputerName\",\"Label\":\"Computer Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleComputerName\"},{\"Key\":{\"Id\":\"TestResult.Duration\",\"Label\":\"Duration\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.TimeSpan\"},\"Value\":\"10675199.02:48:05.4775807\"},{\"Key\":{\"Id\":\"TestResult.StartTime\",\"Label\":\"Start Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"0001-01-01T00:00:00+00:00\"},{\"Key\":{\"Id\":\"TestResult.EndTime\",\"Label\":\"End Time\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.DateTimeOffset\"},\"Value\":\"9999-12-31T23:59:59.9999999+00:00\"}]}";

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
            result.Attachments.Add(new AttachmentSet(new Uri("http://dummyUri"), "sampleAttachment"));
            var expectedJson = "{\"TestCase\":{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"}]},\"Attachments\":[{\"Uri\":\"http://dummyUri\",\"DisplayName\":\"sampleAttachment\",\"Attachments\":[]}],\"Messages\":[],\"Properties\":[]}";

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

        private static string Serialize<T>(T data)
        {
            return JsonDataSerializer.Instance.Serialize(data);
        }

        private static T Deserialize<T>(string json)
        {
            return JsonDataSerializer.Instance.Deserialize<T>(json);
        }
    }
}