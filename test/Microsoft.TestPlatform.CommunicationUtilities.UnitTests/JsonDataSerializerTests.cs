// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonDataSerializerTests
    {
        private JsonDataSerializer jsonDataSerializer;

        public JsonDataSerializerTests()
        {
            this.jsonDataSerializer = JsonDataSerializer.Instance;
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
        public void CloneShouldCloneNewObject()
        {
            var myClass = new MyClass()
            {
                Id = Guid.NewGuid(),
                Name = "Name1",
                Properties = new Dictionary<string, object>()
                {
                    { "PropKey1", 10 },
                    { "PropKey2", "PropValue2" },
                    { "PropKey3", new MyClass() { Id = Guid.NewGuid(), Name = "Name2" } },
                }
            };

            var clonedMyClass = this.jsonDataSerializer.Clone<MyClass>(myClass);

            Console.WriteLine("myClass.Properties[\"PropKey3\"].GetType(): " + myClass.Properties["PropKey3"].GetType());
            Console.WriteLine("clonedMyClass.Properties[\"PropKey3\"].GetType(): " + clonedMyClass.Properties["PropKey3"].GetType());

            Assert.AreNotEqual(myClass.GetHashCode(), clonedMyClass.GetHashCode());

            clonedMyClass.Properties["PropKey1"] = 11;

            Assert.AreEqual(10, myClass.Properties["PropKey1"]);
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

        [DataContract]
        private class MyClass
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public Guid Id { get; set; }

            [DataMember]
            public Dictionary<string, object> Properties { get; set; }
        }
    }
}
