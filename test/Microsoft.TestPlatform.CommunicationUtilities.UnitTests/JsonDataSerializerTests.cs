// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using VisualStudio.TestPlatform.ObjectModel;
    using TestResult = VisualStudio.TestPlatform.ObjectModel.TestResult;

    [TestClass]
    public class JsonDataSerializerTests
    {
        private JsonDataSerializer jsonDataSerializer;

        public JsonDataSerializerTests()
        {
            this.jsonDataSerializer = JsonDataSerializer.Instance;
        }

        [TestMethod]
        public void SerializePayloadShouldNotPickDefaultSettings()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            var classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(null);
            classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(classWithSelfReferencingLoop);
            classWithSelfReferencingLoop.InfiniteRefernce.InfiniteRefernce = classWithSelfReferencingLoop;

            string serializedPayload = this.jsonDataSerializer.SerializePayload("dummy", classWithSelfReferencingLoop);
            Assert.AreEqual("{\"MessageType\":\"dummy\",\"Payload\":{\"InfiniteRefernce\":{}}}", serializedPayload);
        }

        [TestMethod]
        public void DeserializeMessageShouldNotPickDefaultSettings()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            Message message = this.jsonDataSerializer.DeserializeMessage("{\"MessageType\":\"dummy\",\"Payload\":{\"InfiniteRefernce\":{}}}");
            Assert.AreEqual("dummy", message?.MessageType);
        }

        [TestMethod]
        public void SerializePayloadShouldSerializeAnObjectWithSelfReferencingLoop()
        {
            var classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(null);
            classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(classWithSelfReferencingLoop);
            classWithSelfReferencingLoop.InfiniteRefernce.InfiniteRefernce = classWithSelfReferencingLoop;

            // This line should not throw exception
            this.jsonDataSerializer.SerializePayload("dummy", classWithSelfReferencingLoop);
        }

        [TestMethod]
        public void DeserializeShouldDeserializeAnObjectWhichHadSelfReferencingLoopBeforeSerialization()
        {
            var classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(null);
            classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(classWithSelfReferencingLoop);
            classWithSelfReferencingLoop.InfiniteRefernce.InfiniteRefernce = classWithSelfReferencingLoop;

            var json = this.jsonDataSerializer.SerializePayload("dummy", classWithSelfReferencingLoop);

            // This line should deserialize properly
            var result = this.jsonDataSerializer.Deserialize<ClassWithSelfReferencingLoop>(json, 1);

            Assert.AreEqual(typeof(ClassWithSelfReferencingLoop), result.GetType());
            Assert.IsNull(result.InfiniteRefernce);
        }

        [TestMethod]
        public void CloneShouldReturnNullForNull()
        {
            var clonedTestCase = this.jsonDataSerializer.Clone<TestCase>(null);

            Assert.IsNull(clonedTestCase);
        }

        [TestMethod]
        public void CloneShouldWorkForValueType()
        {
            var i = 2;
            var clonedI = this.jsonDataSerializer.Clone<int>(i);

            Assert.AreEqual(clonedI, i);
        }

        [TestMethod]
        public void CloneShouldCloneTestCaseObject()
        {
            var testCase = JsonDataSerializerTests.GetSampleTestCase(out var expectedTrait);

            var clonedTestCase = this.jsonDataSerializer.Clone<TestCase>(testCase);

            VerifyTestCaseClone(clonedTestCase, testCase, expectedTrait);
        }

        [TestMethod]
        public void CloneShouldCloneTestResultsObject()
        {
            var testCase = JsonDataSerializerTests.GetSampleTestCase(out var expectedTrait);

            var testResult = new TestResult(testCase);

            var startTime = DateTimeOffset.UtcNow;
            testResult.StartTime = startTime;

            var clonedTestResult = this.jsonDataSerializer.Clone<TestResult>(testResult);

            Assert.IsFalse(ReferenceEquals(testResult, clonedTestResult));

            Assert.AreEqual(testResult.StartTime, clonedTestResult.StartTime);

            VerifyTestCaseClone(testResult.TestCase, clonedTestResult.TestCase, expectedTrait);
        }

        private static TestCase GetSampleTestCase(out Trait expectedTrait)
        {
            var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");

            expectedTrait = new Trait("TraitName1", "TraitValue1");

            testCase.Traits.Add(expectedTrait);
            return testCase;
        }

        private static void VerifyTestCaseClone(TestCase clonedTestCase, TestCase testCase, Trait expectedTrait)
        {
            Assert.IsFalse(ReferenceEquals(clonedTestCase, testCase));

            Assert.AreEqual(testCase.FullyQualifiedName, clonedTestCase.FullyQualifiedName);
            Assert.IsFalse(ReferenceEquals(testCase.FullyQualifiedName, clonedTestCase.FullyQualifiedName));

            Assert.AreEqual(testCase.ExecutorUri, clonedTestCase.ExecutorUri);
            Assert.IsFalse(ReferenceEquals(testCase.ExecutorUri, clonedTestCase.ExecutorUri));

            Assert.AreEqual(testCase.Source, clonedTestCase.Source);
            Assert.IsFalse(ReferenceEquals(testCase.Source, clonedTestCase.Source));

            Assert.AreEqual(1, clonedTestCase.Traits.Count());

            foreach (var trait in clonedTestCase.Traits)
            {
                Assert.IsFalse(ReferenceEquals(expectedTrait, trait));
                Assert.AreEqual(expectedTrait.Name, trait.Name);
                Assert.AreEqual(expectedTrait.Value, trait.Value);
            }
        }

        public class ClassWithSelfReferencingLoop
        {
            public ClassWithSelfReferencingLoop(ClassWithSelfReferencingLoop ir)
            {
                this.InfiniteRefernce = ir;
            }

            public ClassWithSelfReferencingLoop InfiniteRefernce
            {
                get;
                set;
            }
        }
    }
}
