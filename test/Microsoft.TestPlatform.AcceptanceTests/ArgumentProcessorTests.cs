// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ArgumentProcessorTests : AcceptanceTestBase
    {

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void PassingNoArgumentsToVsTestConsoleShouldPrintHelpMessage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTest(null);

            //Check for help usage, description and arguments text.
            this.StdOutputContains("Usage: vstest.console.exe");
            this.StdOutputContains("Description: Runs tests from the specified files.");
            this.StdOutputContains("Arguments:");

            //Check for help options text
            this.StdOutputContains("Options:");

            //Check for help examples text
            this.StdOutputContains("To run tests: >vstest.console.exe tests.dll");
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void PassingInvalidArgumentsToVsTestConsoleShouldPrintHelpMessage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /badArgument");

            this.InvokeVsTest(arguments);

            //Check for help usage, description and arguments text.
            this.StdOutputContains("Usage: vstest.console.exe");
            this.StdOutputContains("Description: Runs tests from the specified files.");
            this.StdOutputContains("Arguments:");

            //Check for help options text
            this.StdOutputContains("Options:");

            //Check for help examples text
            this.StdOutputContains("To run tests: >vstest.console.exe tests.dll");
        }
    }
}
