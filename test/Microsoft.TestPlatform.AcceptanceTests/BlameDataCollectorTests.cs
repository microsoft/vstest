// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Xml;

    [TestClass]
    public class BlameDataCollectorTests : AcceptanceTestBase
    {
        private readonly string resultsDir;

        public BlameDataCollectorTests()
        {
            this.resultsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(this.resultsDir))
            {
                Directory.Delete(this.resultsDir, true);
            }
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void BlameDataCollectorShouldGiveCorrectTestCaseName(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("BlameUnitTestProject.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            this.InvokeVsTest(arguments);

            this.VaildateOutput();
        }

        private void VaildateOutput()
        {
            bool isAttachmentReceived = false;
            bool isValid = false;
            this.StdErrorContains("BlameUnitTestProject.UnitTest1.TestMethod2");
            this.StdOutputContains("Sequence.xml");
            var resultFiles = Directory.GetFiles(this.resultsDir, "*", SearchOption.AllDirectories);

            foreach(var file in resultFiles)
            {
                if(file.Contains("Sequence.xml"))
                {
                    isAttachmentReceived = true;
                    isValid = IsValidXml(file);
                    break;
                }
            }
            Assert.IsTrue(isAttachmentReceived);
            Assert.IsTrue(isValid);
        }

        private bool IsValidXml(string xmlFilePath)
        {
            var file = File.OpenRead(xmlFilePath);
            var reader = XmlReader.Create(file);
            try
            {
                while (reader.Read())
                {
                }
                file.Dispose();
                return true;
            }
            catch (XmlException)
            {
                file.Dispose();
                return false;
            }
        }
    }
}
