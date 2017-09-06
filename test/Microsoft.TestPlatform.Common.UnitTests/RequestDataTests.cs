// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common;

    [TestClass]
    public class RequestDataTests
    {
        [TestMethod]
        public void ConstructorShouldThrowIfMetricsCollectorIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new RequestData(null));
        }
    }
}
