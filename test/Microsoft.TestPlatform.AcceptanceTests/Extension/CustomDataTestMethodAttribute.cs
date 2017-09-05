// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The custom data test method attribute.
    /// </summary>
    public class CustomDataTestMethodAttribute : TestMethodAttribute
    {
        /// <summary>
        /// Find all data rows and execute.
        /// </summary>
        /// <param name="testMethod">
        /// The test Method.
        /// </param>
        /// <returns>
        /// The <see cref="TestResult[]"/>.
        /// </returns>
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            List<DataRowAttribute> dataRows = new List<DataRowAttribute>();

            var netFullRows = testMethod.GetAttributes<NETFullTargetFramework>(false);
            if (netFullRows != null && netFullRows.Length > 0 && netFullRows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(netFullRows[0].DataRows);
            }

            var netFullInProcessRows = testMethod.GetAttributes<NETFullTargetFrameworkInProcess>(false);
            if (netFullInProcessRows != null && netFullInProcessRows.Length > 0 && netFullInProcessRows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(netFullInProcessRows[0].DataRows);
            }

            var netFullInIsolationRows = testMethod.GetAttributes<NETFullTargetFrameworkInIsolation>(false);
            if (netFullInIsolationRows != null && netFullInIsolationRows.Length > 0 && netFullInIsolationRows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(netFullInIsolationRows[0].DataRows);
            }

            var netcoreappRows = testMethod.GetAttributes<NETCORETargetFramework>(false);
            if (netcoreappRows != null && netcoreappRows.Length > 0 && netcoreappRows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(netcoreappRows[0].DataRows);
            }

            if (dataRows.Count == 0)
            {
                return new TestResult[] { new TestResult() { Outcome = UnitTestOutcome.Failed, TestFailureException = new Exception("No DataRowAttribute specified. Atleast one DataRowAttribute is required with DataTestMethodAttribute.") } };
            }

            return RunDataDrivenTest(testMethod, dataRows.ToArray());
        }

        /// <summary>
        /// Run data driven test method.
        /// </summary>
        /// <param name="testMethod"> Test method to execute. </param>
        /// <param name="dataRows"> Data Row. </param>
        /// <returns> Results of execution. </returns>
        internal static TestResult[] RunDataDrivenTest(ITestMethod testMethod, DataRowAttribute[] dataRows)
        {
            List<TestResult> results = new List<TestResult>();

            foreach (var dataRow in dataRows)
            {
                TestResult result = testMethod.Invoke(dataRow.Data);

                if (!string.IsNullOrEmpty(dataRow.DisplayName))
                {
                    result.DisplayName = dataRow.DisplayName;
                }
                else
                {
                    result.DisplayName = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", testMethod.TestMethodName, string.Join(",", dataRow.Data));
                }

                results.Add(result);
            }

            return results.ToArray();
        }
    }

    /// <summary>
    /// The attribute defining runner framework, target framework and target runtime for net451.
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests. 
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// Third argument (Target runtime) = This get used to find vstest.console dll/exe. 
    /// Fourth argument (InIsolation) = Run the test in isolation(inside testhost) or in process(inside vstest.console.exe)
    /// </summary>
    public class NETFullTargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETFullTargetFramework"/> class.
        /// </summary>
        public NETFullTargetFramework()
        {
            this.DataRows = new List<DataRowAttribute>(1);
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }

    /// <summary>
    /// The attribute defining runner framework, target framework and target runtime for net451 if running in isoalation
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests. 
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// Third argument (Target runtime) = This get used to find vstest.console dll/exe. 
    /// Fourth argument (InIsolation) = Run the test in isolation(inside testhost) or in process(inside vstest.console.exe)
    /// </summary>
    public class NETFullTargetFrameworkInIsolation : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETFullTargetFramework"/> class.
        /// </summary>
        public NETFullTargetFrameworkInIsolation()
        {
            this.DataRows = new List<DataRowAttribute>(1);
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime, AcceptanceTestBase.InIsolation));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }

    /// <summary>
    /// The attribute defining runner framework, target framework and target runtime for net451 if running in process
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests. 
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// Third argument (Target runtime) = This get used to find vstest.console dll/exe. 
    /// Fourth argument (InIsolation) = Run the test in isolation(inside testhost) or in process(inside vstest.console.exe)
    /// </summary>
    public class NETFullTargetFrameworkInProcess : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETFullTargetFramework"/> class.
        /// </summary>
        public NETFullTargetFrameworkInProcess()
        {
            this.DataRows = new List<DataRowAttribute>(1);
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }

    /// <summary>
    /// The attribute defining runner framework, target framework and target runtime for netcoreapp1.*
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests. 
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// Third argument (Target runtime) = This get used to find vstest.console dll/exe. 
    /// Fourth argument (InIsolation) = Run the test in isolation(inside testhost) or in process(inside vstest.console.exe)
    /// </summary>
    public class NETCORETargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETCORETargetFramework"/> class.
        /// </summary>
        public NETCORETargetFramework()
        {
            this.DataRows = new List<DataRowAttribute>(6);
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.CoreTargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.CoreTargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core11TargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core11TargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core20TargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core20TargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime, AcceptanceTestBase.NoIsolation));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }
}
