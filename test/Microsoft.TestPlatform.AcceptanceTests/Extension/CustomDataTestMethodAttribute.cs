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

            var netcoreappRows = testMethod.GetAttributes<NETCORETargetFramework>(false);
            if (netcoreappRows != null && netcoreappRows.Length > 0 && netcoreappRows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(netcoreappRows[0].DataRows);
            }

            var netDesktopRows = testMethod.GetAttributes<NETDesktopTargetFramework>(false);
            if (netDesktopRows != null && netDesktopRows.Length > 0 && netDesktopRows[0].DataRows.Count > 0)
            {
                dataRows.AddRange(netDesktopRows[0].DataRows);
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
                    var runnnerInfo = (RunnnerInfo)dataRow.Data[0];
                    result.DisplayName = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", testMethod.TestMethodName, runnnerInfo.ToString());
                }

                results.Add(result);
            }

            return results.ToArray();
        }
    }

    /// <summary>
    /// The attribute defining runner framework and target framework for net451.
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests. 
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// </summary>
    public class NETFullTargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETFullTargetFramework"/> class.
        /// </summary>
        /// <param name="inIsolation">Run test in isolation</param>
        /// <param name="inProcess">Run tests in process</param>
        public NETFullTargetFramework(bool inIsolation = true, bool inProcess = false)
        {
            this.DataRows = new List<DataRowAttribute>();
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.DesktopTargetFramework)));

            if (inIsolation == true)
            {
                this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.InIsolation)));
            }

            if (inProcess == true)
            {
                this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework)));
            }
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
    /// </summary>
    public class NETCORETargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETCORETargetFramework"/> class.
        /// </summary>
        public NETCORETargetFramework()
        {
            this.DataRows = new List<DataRowAttribute>(6);
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.CoreTargetFramework)));
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.CoreTargetFramework)));
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core11TargetFramework)));
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core11TargetFramework)));
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core20TargetFramework)));
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core20TargetFramework)));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }

    /// <summary>
    /// The attribute defining both runner framework and target framework as net451.
    /// </summary>
    public class NETDesktopTargetFramework : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NETDesktopTargetFramework"/> class.
        /// </summary>
        public NETDesktopTargetFramework()
        {
            this.DataRows = new List<DataRowAttribute>(1);
            this.DataRows.Add(new DataRowAttribute(new RunnnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework)));
        }

        /// <summary>
        /// Gets or sets the data rows.
        /// </summary>
        public List<DataRowAttribute> DataRows { get; set; }
    }
}
