﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ArgumentProcessorTests : AcceptanceTestBase
    {

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
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

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void PassingInvalidArgumentsToVsTestConsoleShouldNotPrintHelpMessage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /badArgument");

            this.InvokeVsTest(arguments);

            //Check for help usage, description and arguments text.
            this.StdOutputDoesNotContains("Usage: vstest.console.exe");
            this.StdOutputDoesNotContains("Description: Runs tests from the specified files.");
            this.StdOutputDoesNotContains("Arguments:");

            //Check for help options text
            this.StdOutputDoesNotContains("Options:");

            //Check for help examples text
            this.StdOutputDoesNotContains("To run tests: >vstest.console.exe tests.dll");

            //Check for message which guides using help option
            this.StdErrorContains("Please use the /help option to check the list of valid arguments");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void PassingInvalidArgumentsToVsTestConsoleShouldPrintHowToUseHelpMessage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /badArgument");

            this.InvokeVsTest(arguments);

            //Check for message which guides using help option
            this.StdErrorContains("Please use the /help option to check the list of valid arguments");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void PassingInvalidArgumentWithPrefixToVsTestConsoleShouldPrintHowToUseHelpMessage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, "-");

            this.InvokeVsTest(arguments);

            //Check for message which guides using help option
            this.StdErrorContains("Please use the /help option to check the list of valid arguments");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void PassingInvalidArgumentWithAValueToVsTestConsoleShouldPrintHowToUseHelpMessage(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, "/badArgument:abc");

            this.InvokeVsTest(arguments);

            //Check for message which guides using help option
            this.StdErrorContains("Please use the /help option to check the list of valid arguments");
        }
    }
}
