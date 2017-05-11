// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonDataSerializerTests
    {
        [TestMethod]
        public void SerializePayloadShouldSerializeAnObjectWithSelfReferencingLoop()
        {
            var classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(null);
            classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(classWithSelfReferencingLoop);
            classWithSelfReferencingLoop.InfiniteRefernce.InfiniteRefernce = classWithSelfReferencingLoop;

            var sut = JsonDataSerializer.Instance;

            // This line should not throw exception
            sut.SerializePayload("dummy", classWithSelfReferencingLoop);
        }

        [TestMethod]
        public void DeserializeShouldDeserializeAnObjectWhichHadSelfReferencingLoopBeforeSerialization()
        {
            var classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(null);
            classWithSelfReferencingLoop = new ClassWithSelfReferencingLoop(classWithSelfReferencingLoop);
            classWithSelfReferencingLoop.InfiniteRefernce.InfiniteRefernce = classWithSelfReferencingLoop;

            var sut = JsonDataSerializer.Instance;

            var json = sut.SerializePayload("dummy", classWithSelfReferencingLoop);

            // This line should deserialize properly
            var result = sut.Deserialize<ClassWithSelfReferencingLoop>(json, 1);

            Assert.AreEqual(typeof(ClassWithSelfReferencingLoop), result.GetType());
            Assert.IsNull(result.InfiniteRefernce);
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
