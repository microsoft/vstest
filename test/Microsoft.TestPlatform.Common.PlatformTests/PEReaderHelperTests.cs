// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Utilities
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PEReaderHelperTests : IntegrationTestBase
    {
        private PEReaderHelper peReaderHelper;

        public PEReaderHelperTests()
        {
            this.peReaderHelper = new PEReaderHelper();
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetAssemblyTypeForManagedDll(string framework)
        {
            var assemblyPath = this.testEnvironment.GetTestAsset("SimpleTestProject3.dll", framework);
            var assemblyType = this.peReaderHelper.GetAssemblyType(assemblyPath);

            Assert.AreEqual(AssemblyType.Managed, assemblyType);
        }

        [TestMethod]
        public void GetAssemblyTypeForNativeDll()
        {
            var assemblyPath = $@"{this.testEnvironment.PackageDirectory}\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var assemblyType = this.peReaderHelper.GetAssemblyType(assemblyPath);

            Assert.AreEqual(AssemblyType.Native, assemblyType);
        }

        [TestMethod]
        public void GetAssemblyTypeForManagedExe()
        {
            var assemblyPath = this.testEnvironment.GetTestAsset("ConsoleManagedApp.exe", "net451");
            var assemblyType = this.peReaderHelper.GetAssemblyType(assemblyPath);

            Assert.AreEqual(AssemblyType.Managed, assemblyType);
        }

        [TestMethod]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetAssemblyTypeForNetCoreManagedExe(string framework)
        {
            var assemblyPath = this.testEnvironment.GetTestAsset("ConsoleManagedApp.dll", framework);
            var assemblyType = this.peReaderHelper.GetAssemblyType(assemblyPath);

            Assert.AreEqual(AssemblyType.Managed, assemblyType);
        }

        [TestMethod]
        public void GetAssemblyTypeForNativeExe()
        {
            var assemblyPath = $@"{this.testEnvironment.PackageDirectory}\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.ConsoleNativeApp.exe";
            var assemblyType = this.peReaderHelper.GetAssemblyType(assemblyPath);

            Assert.AreEqual(AssemblyType.Native, assemblyType);
        }

        [TestMethod]
        public void GetAssemblyTypeShouldReturnNoneInCaseOfError()
        {
            var assemblyType = this.peReaderHelper.GetAssemblyType("invalidFile.dll");

            Assert.AreEqual(AssemblyType.None, assemblyType);
        }
    }
}
