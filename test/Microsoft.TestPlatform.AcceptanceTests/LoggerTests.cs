﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LoggerTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFrameworkWithCoreRunner]
        [NETFullTargetFrameworkInProcess]
        [NETFullTargetFrameworkInIsolation]
        [NETCORETargetFramework]
        public void TrxLoggerShouldProperlyOverwriteFile(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, inIsolation);
            var trxFileName = "TestResults.trx";
            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            this.InvokeVsTest(arguments);

            arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, inIsolation);
            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
            this.InvokeVsTest(arguments);

            var trxLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults",  trxFileName);
            Assert.IsTrue(IsValidXml(trxLogFilePath), "Invalid content in Trx log file");
        }

        private bool IsValidXml(string xmlFilePath)
        {
            var reader = System.Xml.XmlReader.Create(File.OpenRead(xmlFilePath));
            try
            {
                while (reader.Read())
                {
                }
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}
