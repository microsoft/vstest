// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SimpleTestProjectFramework35
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Class1
    {
        [TestMethod]
        public void PassingTest()
        {
            Assert.AreEqual(2, 2);
        }
    }
}
