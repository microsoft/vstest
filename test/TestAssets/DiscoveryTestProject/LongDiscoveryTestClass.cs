// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

namespace DiscoveryTestProject3
{
    [TestClass]
    public class LongDiscoveryTestClass
    {
        [MyTestMethod]
        public void CustomTestMethod()
        {

        }
    }

    internal class MyTestMethodAttribute : TestMethodAttribute
    {
        public MyTestMethodAttribute()
        {
            Thread.Sleep(10000);
        }
    }
}
