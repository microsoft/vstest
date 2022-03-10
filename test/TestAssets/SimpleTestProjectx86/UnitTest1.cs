// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SimpleTestProjectx86
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The unit test 1.
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// The passing test.
        /// </summary>
        [TestMethod]
        public void PassingTestx86()
        {
            Assert.AreEqual(2, 2);
        }
    }
}
