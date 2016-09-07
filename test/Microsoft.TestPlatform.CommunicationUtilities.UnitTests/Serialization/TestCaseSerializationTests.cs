﻿// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CommunicationUtilities.UnitTests.Serialization
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class TestCaseSerializationTests
    {
        private static TestCase testCase = new TestCase(
                                               "sampleTestClass.sampleTestCase",
                                               new Uri("executor://sampleTestExecutor"),
                                               "sampleTest.dll")
                                               {
                                                   CodeFilePath = "/user/src/testFile.cs",
                                                   DisplayName = "sampleTestCase",
                                                   Id = Guid.Empty,
                                                   LineNumber = 999,
                                                   Traits = { new Trait("Priority", "0"), new Trait("Category", "unit") }
                                               };

        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                                                                       {
                                                                           ContractResolver = new TestPlatformContractResolver(),
                                                                           TypeNameHandling = TypeNameHandling.None
                                                                       };

        [TestMethod]
        public void TestCaseJsonShouldContainAllPropertiesOnSerialization()
        {
            var json = Serialize(testCase);

            // Use raw deserialization to validate basic properties
            dynamic data = JObject.Parse(json);
            dynamic properties = data["Properties"];
            Assert.AreEqual("TestCase.FullyQualifiedName", properties[0]["Key"]["Id"].Value);
            Assert.AreEqual("sampleTestClass.sampleTestCase", properties[0]["Value"].Value);
            Assert.AreEqual("TestCase.ExecutorUri", properties[1]["Key"]["Id"].Value);
            Assert.AreEqual("executor://sampleTestExecutor", properties[1]["Value"].Value);
            Assert.AreEqual("TestCase.Source", properties[2]["Key"]["Id"].Value);
            Assert.AreEqual("sampleTest.dll", properties[2]["Value"].Value);
            Assert.AreEqual("TestCase.CodeFilePath", properties[3]["Key"]["Id"].Value);
            Assert.AreEqual("/user/src/testFile.cs", properties[3]["Value"].Value);
            Assert.AreEqual("TestCase.DisplayName", properties[4]["Key"]["Id"].Value);
            Assert.AreEqual("sampleTestCase", properties[4]["Value"].Value);
            Assert.AreEqual("TestCase.Id", properties[5]["Key"]["Id"].Value);
            Assert.AreEqual("00000000-0000-0000-0000-000000000000", properties[5]["Value"].Value);
            Assert.AreEqual("TestCase.LineNumber", properties[6]["Key"]["Id"].Value);
            Assert.AreEqual(999, properties[6]["Value"].Value);

            // Traits require special handling with TestPlatformContract resolver. It should be null without it.
            Assert.AreEqual("TestObject.Traits", properties[7]["Key"]["Id"].Value);
            Assert.IsNull(properties[7]["Key"]["Value"]);
        }

        [TestMethod]
        public void TestCaseObjectShouldContainAllPropertiesOnDeserialization()
        {
            var json = "{\"Properties\":[{\"Key\":{\"Id\":\"TestCase.FullyQualifiedName\",\"Label\":\"FullyQualifiedName\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestClass.sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.ExecutorUri\",\"Label\":\"Executor Uri\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Uri\"},\"Value\":\"executor://sampleTestExecutor\"},{\"Key\":{\"Id\":\"TestCase.Source\",\"Label\":\"Source\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTest.dll\"},{\"Key\":{\"Id\":\"TestCase.CodeFilePath\",\"Label\":\"File Path\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"/user/src/testFile.cs\"},{\"Key\":{\"Id\":\"TestCase.DisplayName\",\"Label\":\"Name\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.String\"},\"Value\":\"sampleTestCase\"},{\"Key\":{\"Id\":\"TestCase.Id\",\"Label\":\"Id\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Guid\"},\"Value\":\"00000000-0000-0000-0000-000000000000\"},{\"Key\":{\"Id\":\"TestCase.LineNumber\",\"Label\":\"Line Number\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":1,\"ValueType\":\"System.Int32\"},\"Value\":999},{\"Key\":{\"Id\":\"TestObject.Traits\",\"Label\":\"Traits\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":5,\"ValueType\":\"System.Collections.Generic.KeyValuePair`2[[System.String],[System.String]][]\"},\"Value\":[{\"Key\":\"Priority\",\"Value\":\"0\"},{\"Key\":\"Category\",\"Value\":\"unit\"}]}]}";
            var test = Deserialize<TestCase>(json);

            Assert.AreEqual(testCase.CodeFilePath, test.CodeFilePath);
            Assert.AreEqual(testCase.DisplayName, test.DisplayName);
            Assert.AreEqual(testCase.ExecutorUri, test.ExecutorUri);
            Assert.AreEqual(testCase.FullyQualifiedName, test.FullyQualifiedName);
            Assert.AreEqual(testCase.LineNumber, test.LineNumber);
            Assert.AreEqual(testCase.Source, test.Source);
            Assert.AreEqual(testCase.Traits.First().Name, test.Traits.First().Name);
        }

        private static string Serialize<T>(T data)
        {
            return JsonConvert.SerializeObject(data, serializerSettings);
        }

        private static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }
    }
}