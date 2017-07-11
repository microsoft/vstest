// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.TestPlatform.TestUtilities.PerfInstrumentation;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileHelperTests : PerformanceTestBase
    {
        [TestMethod]
        public void EnumerateFilesPerfTests()
        {
            var fileHelper = new FileHelper();
            var stopwatch = new Stopwatch();
            var adapterPath = Directory.GetCurrentDirectory();
            var patterns = new string[]
                              {
                                   ".*CoreUtilities.dll",
                                   ".*CommunicationUtilities.dll",
                                   ".*CrossPlatEngine.dll",
                                   ".*TestUtilities.dll"
                              };

            stopwatch.Start();
            var extensionAssemblies =
                new List<string>(
                    fileHelper.EnumerateFiles(
                        adapterPath,
                       patterns[0],
                        SearchOption.AllDirectories));
            extensionAssemblies.AddRange(
                fileHelper.EnumerateFiles(
                    adapterPath,
                    patterns[1],
                    SearchOption.AllDirectories));
            extensionAssemblies.AddRange(
                fileHelper.EnumerateFiles(
                    adapterPath,
                    patterns[2],
                    SearchOption.AllDirectories));
            extensionAssemblies.AddRange(
                fileHelper.EnumerateFiles(
                    adapterPath,
                    patterns[3],
                    SearchOption.AllDirectories));

            stopwatch.Stop();
            var timeForOldApi = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var extensionAssembliesNew =
                new List<string>(fileHelper.EnumerateFiles(adapterPath, patterns, SearchOption.AllDirectories));
            stopwatch.Stop();
            var timeForNewApi = stopwatch.ElapsedMilliseconds;

            Assert.AreEqual(extensionAssemblies.Count, extensionAssembliesNew.Count);
            Assert.IsTrue(timeForNewApi < timeForOldApi);
        }
    }
}
