// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MstestV1UnitTestProject
{
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.VisualStudio.TestTools.UnitTesting;


    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// The passing test.
        /// </summary>
        [Priority(2)]
        [TestMethod]
        public void PassingTest1()
        {
            Assert.AreEqual(2, 2);
        }

        [TestMethod]
        public void PassingTest2()
        {
            Assert.AreEqual(3, 3);
        }

        /// <summary>
        /// The failing test.
        /// </summary>
        [TestCategory("CategoryA")]
        [Priority(3)]
        [TestMethod]
        public void FailingTest1()
        {
            Assert.AreEqual(2, 3);
        }

        [TestMethod]
        public void FailingTest2()
        {
            Assert.AreEqual(2, 4);
        }

        /// <summary>
        /// The skipping test.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SkippingTest()
        {
        }
    }
}
