// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;

    [TestClass]
    public class FileHelperTests
    {
        [TestMethod]
        public void GetNextIterationFileNameShouldReplaceWhiteSpaceWithUnderScore()
        {
            string expected = "TempDirectory\\User_2016-12-06_15_45_56.trx";
            string result = FileHelper.GetNextIterationFileName("TempDirectory", "User 2016-12-06 15_45_56.trx", false);

            Assert.AreEqual(expected, result);
        }
    }
}
