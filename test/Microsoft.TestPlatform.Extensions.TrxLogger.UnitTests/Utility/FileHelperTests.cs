// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests.Utility
{
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileHelperTests
    {
        [TestMethod]
        public void ReplaceInvalidFileNameCharsShouldReplaceSpace()
        {
            Assert.AreEqual("samadala_SAMADALA_2017-10-13_18_33_17",
                FileHelper.ReplaceInvalidFileNameChars("samadala_SAMADALA 2017-10-13 18_33_17"));
        }
    }
}
