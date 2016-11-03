// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /*[TestClass]*/

    // TODO enable netcore test when test asset project migrate to csproj

    /// <summary>
    /// Acceptance tests for netcore framework
    /// </summary>
    public class NetCoreExecutionTest : ExecutionTests
    {
        [TestInitialize]
        public void SetTestFrameWork()
        {
            this.Framework = ".NETCoreApp,Version=v1.0";
            this.testEnvironment.TargetFramework = "netcoreapp1.0";
        }
    }
}
