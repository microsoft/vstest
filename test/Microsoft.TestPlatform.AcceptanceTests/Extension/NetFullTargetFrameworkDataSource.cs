// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using TestUtilities;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The attribute defining runner framework and target framework for net451.
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests.
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// </summary>
    public class NetFullTargetFrameworkDataSource : Attribute, ITestDataSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetFullTargetFrameworkDataSource"/> class.
        /// </summary>
        /// <param name="inIsolation">Run test in isolation</param>
        /// <param name="inProcess">Run tests in process</param>
        /// <param name="useDesktopRunner">To run tests with desktop runner(vstest.console.exe)</param>
        /// <param name="useCoreRunner">To run tests with core runner(dotnet vstest.console.dll)</param>
        public NetFullTargetFrameworkDataSource(bool inIsolation = true, bool inProcess = false, bool useDesktopRunner = true, bool useCoreRunner = true)
        {
            this.dataRows = new List<object[]>();

            if (useCoreRunner)
            {
                this.dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.DesktopTargetFramework) });
            }

            if (useDesktopRunner)
            {
                if (inIsolation)
                {
                    this.dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.InIsolation) });
                }

                if (inProcess)
                {
                    this.dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework) });
                }
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="NetCoreTargetFrameworkDataSource"/> class.
        /// </summary>
        /// <param name="targetFrameworks">To run tests with desktop runner(vstest.console.exe), use AcceptanceTestBase.Net452TargetFramework or alike values.</param>
        public NetFullTargetFrameworkDataSource(string[] targetFrameworks, bool inIsolation = true, bool inProcess = false)
        {
            if (inIsolation)
            {
                foreach (var fmw in targetFrameworks)
                {
                    this.dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, fmw, AcceptanceTestBase.InIsolation) });
                }
            }

            if (inProcess)
            {
                foreach (var fmw in targetFrameworks)
                {
                    this.dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, fmw) });
                }
            }
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        private List<object[]> dataRows = new List<object[]>();

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            return this.dataRows;
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
        }
    }
}