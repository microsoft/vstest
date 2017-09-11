// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestExecutor.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.TestExecutor;

    [TestClass]
    public class UnitTestClientTests
    {
        [TestMethod]
        public void SplitArgumentsShouldHonorDoubleQuotes()
        {
            var argument = "--port 8080 --endpoint 127.0.0.1:8020 --diag \"abc txt\"";
            string[] argsArr = UnitTestClient.SplitArguments(argument);

            Assert.AreEqual(argsArr.Length, 6);
        }

        [TestMethod]
        public void SplitArgumentsShouldHonorSingleQuotes()
        {
            var argument = "--port 8080 --endpoint 127.0.0.1:8020 --diag \'abc txt\'";
            string[] argsArr = UnitTestClient.SplitArguments(argument);

            Assert.AreEqual(argsArr.Length, 6);
        }

        [TestMethod]
        public void SplitArgumentsShouldSplitAtSpacesOutsideOfQuotes()
        {
            var argument = "--port 8080 --endpoint 127.0.0.1:8020 --diag abc txt";
            string[] argsArr = UnitTestClient.SplitArguments(argument);

            Assert.AreEqual(argsArr.Length, 7);
        }
    }
}
