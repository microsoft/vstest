// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcDumpArgsBuilderTests
    {
        private int defaultProcId = 1234;
        private string defaultDumpFileName = "dump";

        [TestMethod]
        public void BuildHangBasedProcDumpArgsShouldCreateCorrectArgString()
        {
            var procDumpArgsBuilder = new ProcDumpArgsBuilder();
            var argString = procDumpArgsBuilder.BuildHangBasedProcDumpArgs(this.defaultProcId, this.defaultDumpFileName, false);
            Assert.AreEqual("-accepteula -n 1 1234 dump.dmp", argString);
        }

        [TestMethod]
        public void BuildHangBasedProcDumpArgsWithFullDumpEnabledShouldCreateCorrectArgString()
        {
            var procDumpArgsBuilder = new ProcDumpArgsBuilder();
            var argString = procDumpArgsBuilder.BuildHangBasedProcDumpArgs(this.defaultProcId, this.defaultDumpFileName, true);
            Assert.AreEqual("-accepteula -n 1 -ma 1234 dump.dmp", argString);
        }

        [TestMethod]
        public void BuildTriggerBasedProcDumpArgsShouldCreateCorrectArgString()
        {
            var procDumpArgsBuilder = new ProcDumpArgsBuilder();
            var argString = procDumpArgsBuilder.BuildTriggerBasedProcDumpArgs(this.defaultProcId, this.defaultDumpFileName, new List<string> { "a", "b" }, false);
            Assert.AreEqual("-accepteula -e 1 -g -t -f a -f b 1234 dump.dmp", argString);
        }

        [TestMethod]
        public void BuildTriggerProcDumpArgsWithFullDumpEnabledShouldCreateCorrectArgString()
        {
            var procDumpArgsBuilder = new ProcDumpArgsBuilder();
            var argString = procDumpArgsBuilder.BuildTriggerBasedProcDumpArgs(this.defaultProcId, this.defaultDumpFileName, new List<string> { "a", "b" }, true);
            Assert.AreEqual("-accepteula -e 1 -g -t -ma -f a -f b 1234 dump.dmp", argString);
        }
    }
}
