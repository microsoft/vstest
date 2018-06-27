// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Helpers
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using VisualStudio.TestPlatform.Utilities.Helpers;

    [TestClass]
    public class FileHelperTests
    {
        private readonly FileHelper fileHelper;
        private readonly string tempFile;

        public FileHelperTests()
        {
            this.tempFile = Path.GetTempFileName();
            File.AppendAllText(this.tempFile, "Some content..");
            this.fileHelper = new FileHelper();
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(this.tempFile);
        }

        [TestMethod]
        public void GetStreamShouldAbleToGetTwoStreamSimultanouslyIfFileAccessIsRead()
        {
            using (var stream1 = this.fileHelper.GetStream(this.tempFile, FileMode.Open, FileAccess.Read))
            {
                using (var stream2 =
                    this.fileHelper.GetStream(this.tempFile, FileMode.Open, FileAccess.Read))
                {
                }
            }
        }
    }
}
