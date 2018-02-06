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
    /// The attribute defining runner framework, target framework and target runtime for netcoreapp1.*
    /// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests. 
    /// If runner framework is net46 then vstest.console.exe will run the tests.
    /// Second argument (target framework) = The framework for which test will run
    /// </summary>
    public class NetCoreTargetFrameworkDataSource : Attribute, ITestDataSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetCoreTargetFrameworkDataSource"/> class.
        /// </summary>
        public NetCoreTargetFrameworkDataSource()
        {
            this.dataRows = new List<object[]>(6);
            this.dataRows.Add(new object[] {new RunnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.CoreTargetFramework)});
            this.dataRows.Add(new object[] {new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.CoreTargetFramework)});
            this.dataRows.Add(new object[] {new RunnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core11TargetFramework)});
            this.dataRows.Add(new object[] {new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core11TargetFramework)});
            this.dataRows.Add(new object[] {new RunnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.Core20TargetFramework)});
            this.dataRows.Add(new object[] {new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.Core20TargetFramework)});
        }

        private List<object[]> dataRows;

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