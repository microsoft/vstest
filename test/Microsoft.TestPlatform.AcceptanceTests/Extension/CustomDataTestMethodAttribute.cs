// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.TestPlatform.TestUtilities;

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

            var net46Rows = testMethod.GetAttributes<NET46TargetFramework>(false);
            if (net46Rows != null && net46Rows.Length > 0 && net46Rows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(net46Rows[0].DataRows);
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
    /// </summary>
    public class NET46TargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NET46TargetFramework"/> class.
        /// </summary>
        public NET46TargetFramework()
        {
            this.DataRows = new List<DataRowAttribute>(2);
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }

    /// <summary>
    /// The attribute defining runner framework, target framework and target runtime for netcoreapp1.*
    /// </summary>
    public class NETCORETargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETCORETargetFramework"/> class.
        /// </summary>
        public NETCORETargetFramework()
        {
            this.DataRows = new List<DataRowAttribute>(4);
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.CoreTargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.CoreTargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core11TargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core11TargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core20TargetFramework, AcceptanceTestBase.CoreRunnerTargetRuntime));
            this.DataRows.Add(new DataRowAttribute(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core20TargetFramework, AcceptanceTestBase.DesktopRunnerTargetRuntime));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }
}
