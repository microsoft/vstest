// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Build.UnitTests
{
    using System.Linq;

    using Microsoft.TestPlatform.Build.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VsTestTaskTests
    {
        [TestMethod]
        public void CreateArgumentShouldAddDoubleQuotesForCLIRunSettings()
        {
            const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
            const string arg2 = "MSTest.DeploymentEnabled";
            var vstestTask = new VSTestTask { VSTestCLIRunSettings = new string[2] };
            vstestTask.VSTestCLIRunSettings[0] = arg1;
            vstestTask.VSTestCLIRunSettings[1] = arg2;
            
            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var result = vstestTask.CreateArgument().ToArray();

            // First, second and third args would be --framework:abc, testfilepath and -- respectively.
            Assert.AreEqual($"\"{arg1}\"", result[3]);
            Assert.AreEqual($"\"{arg2}\"", result[4]);
        }
    }
}
