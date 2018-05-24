// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.SettingsMigrator.UnitTests
{
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.SettingsMigrator;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void OnlyOneArgumentShouldBeAccepted()
        {
            int returnCode = Program.Main(new string[] { "asd", "asd" });
            Assert.AreEqual(1, returnCode, "Only one argument should be accepted.");
        }
    }
}