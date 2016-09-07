﻿// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CommunicationUtilities.UnitTests.Serialization
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class TestObjectConverterTests
    {
        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                                                                       {
                                                                           ContractResolver = new TestPlatformContractResolver(),
                                                                           TypeNameHandling = TypeNameHandling.None
                                                                       };

        [TestMethod]
        public void TestObjectJsonShouldContainOnlyProperties()
        {
            var json = Serialize(new TestableTestObject());

            Assert.AreEqual("{\"Properties\":[]}", json);
        }

        [TestMethod]
        public void TestObjectShouldCreateDefaultObjectOnDeserializationOfJsonWithEmptyProperties()
        {
            var test = Deserialize<TestableTestObject>("{\"Properties\":[]}");

            Assert.IsNotNull(test);
            Assert.AreEqual(0, test.Properties.Count());
        }

        [TestMethod]
        public void TestCaseObjectShouldSerializeCustomProperties()
        {
            var test = new TestableTestObject();
            var testProperty1 = TestProperty.Register("1", "label1", typeof(Guid), typeof(TestableTestObject));
            var testPropertyData1 = Guid.Parse("02048dfd-3da7-475d-a011-8dd1121855ec");
            var testProperty2 = TestProperty.Register("2", "label2", typeof(int), typeof(TestableTestObject));
            var testPropertyData2 = 29;
            test.SetPropertyValue(testProperty1, testPropertyData1);
            test.SetPropertyValue(testProperty2, testPropertyData2);

            var json = Serialize(test);

            // Use raw deserialization to validate basic properties
            var expectedJson = "{\"Properties\":[{\"Key\":{\"Id\":\"1\",\"Label\":\"label1\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.Guid\"},\"Value\":\"02048dfd-3da7-475d-a011-8dd1121855ec\"},{\"Key\":{\"Id\":\"2\",\"Label\":\"label2\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.Int32\"},\"Value\":29}]}";
            Assert.AreEqual(expectedJson, json);
        }

        [TestMethod]
        public void TestObjectShouldDeserializeCustomProperties()
        {
            var json = "{\"Properties\":[{\"Key\":{\"Id\":\"1\",\"Label\":\"label1\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.Guid\"},\"Value\":\"02048dfd-3da7-475d-a011-8dd1121855ec\"},{\"Key\":{\"Id\":\"2\",\"Label\":\"label2\",\"Category\":\"\",\"Description\":\"\",\"Attributes\":0,\"ValueType\":\"System.Int32\"},\"Value\":29}]}";

            var test = Deserialize<TestableTestObject>(json);

            var properties = test.Properties.ToArray();
            Assert.AreEqual(2, properties.Length);
            Assert.AreEqual(Guid.Parse("02048dfd-3da7-475d-a011-8dd1121855ec"), test.GetPropertyValue(properties[0]));
            Assert.AreEqual(29, test.GetPropertyValue(properties[1]));
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

    [DataContract]
    internal class TestableTestObject : TestObject
    {
    }
}