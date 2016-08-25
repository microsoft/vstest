// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestPlatform.CommandLine.Processors.Utilities;

    [TestClass]
    public class AdapterSourceMapUtilitiesTests
    {
        [TestMethod]
        public void GetTestRunnerAndAssemblyInfoForNonJsonSourcesReturnsSourceDictWithUndefinedTestRunnerKey()
        {
            string[] sources = { "foo.dll", "foo2.dll" };
            var resultDict = AdapterSourceMapUtilities.GetTestRunnerAndAssemblyInfo(sources);

            IEnumerable<string> result;
            resultDict.TryGetValue("_none_", out result);
            Assert.AreEqual("foo.dll", result.ToArray()[0]);
            Assert.AreEqual("foo2.dll", result.ToArray()[1]);
        }
    }
}
