// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestDiscovererPluginInformationTests
    {
        private TestDiscovererPluginInformation testPluginInformation;
        
        [TestMethod]
        public void AssemblyQualifiedNameShouldReturnTestExtensionTypesName()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.AreEqual(typeof(DummyTestDiscovererWithNoFileExtensions).AssemblyQualifiedName, this.testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnTestExtensionTypesName()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.AreEqual(typeof(DummyTestDiscovererWithNoFileExtensions).AssemblyQualifiedName, this.testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void FileExtensionsShouldReturnEmptyListIfADiscovererSupportsNoFileExtensions()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.IsNotNull(this.testPluginInformation.FileExtensions);
            Assert.AreEqual(0, this.testPluginInformation.FileExtensions.Count);
        }

        [TestMethod]
        public void FileExtensionsShouldReturnAFileExtensionForADiscoverer()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithOneFileExtensions));
            CollectionAssert.AreEqual(new List<string> { "csv"}, this.testPluginInformation.FileExtensions);
        }

        [TestMethod]
        public void FileExtensionsShouldReturnSupportedFileExtensionsForADiscoverer()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithTwoFileExtensions));
            CollectionAssert.AreEqual(new List<string> {"csv", "docx"}, this.testPluginInformation.FileExtensions);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasNoCategory()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithNoCategory));
            Assert.AreEqual(AssemblyType.None, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasCategoryWithNoValue()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithCategoryHavingNoValue));
            Assert.AreEqual(AssemblyType.None, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasCategoryWithEmptyValue()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithCategoryHavingEmptyValue));
            Assert.AreEqual(AssemblyType.None, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNativeIfDiscovererHasNativeCategory()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithNativeCategory));
            Assert.AreEqual(AssemblyType.Native, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnManagedIfDiscovererHasManagedCategory()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithManagedCategory));
            Assert.AreEqual(AssemblyType.Managed, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasUnknownCategory()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithUnknownCategory));
            Assert.AreEqual(AssemblyType.None, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnAssemblyTypeIfDiscovererHasCategoryInArbitCasing()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithArbitCasedCategory));
            Assert.AreEqual(AssemblyType.Native, this.testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void DefaultExecutorUriShouldReturnEmptyListIfADiscovererDoesNotHaveOne()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.IsNotNull(this.testPluginInformation.DefaultExecutorUri);
            Assert.AreEqual(string.Empty, this.testPluginInformation.DefaultExecutorUri);
        }

        [TestMethod]
        public void DefaultExecutorUriShouldReturnDefaultExecutorUriOfADiscoverer()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithOneFileExtensions));
            Assert.AreEqual("csvexecutor", this.testPluginInformation.DefaultExecutorUri);
        }

        [TestMethod]
        public void MetadataShouldReturnFileExtensionsAndDefaultExecutorUriAndAssemblyType()
        {
            this.testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithTwoFileExtensions));

            var expectedFileExtensions = new List<string> { "csv", "docx" };
            var testPluginMetada = this.testPluginInformation.Metadata.ToArray();

            CollectionAssert.AreEqual(expectedFileExtensions, (testPluginMetada[0] as List<string>).ToArray());
            Assert.AreEqual("csvexecutor", testPluginMetada[1] as string);
            Assert.AreEqual(AssemblyType.Managed, Enum.Parse(typeof(AssemblyType), testPluginMetada[2].ToString()));
        }
    }

    #region Implementation
    
    public class DummyTestDiscovererWithNoFileExtensions
    {
    }

    [FileExtension(".dll")]
    public class DummyTestDiscovereWithNoCategory
    {
    }

    [FileExtension(".dll")]
    [Category]
    public class DummyTestDiscovereWithCategoryHavingNoValue
    {
    }

    [FileExtension(".dll")]
    [Category]
    public class DummyTestDiscovereWithCategoryHavingEmptyValue
    {
    }

    [FileExtension(".js")]
    [Category("native")]
    public class DummyTestDiscovereWithNativeCategory
    {
    }

    [FileExtension(".dll")]
    [Category("managed")]
    public class DummyTestDiscovereWithManagedCategory
    {
    }

    [FileExtension(".dll")]
    [Category("arbitValue")]
    public class DummyTestDiscovereWithUnknownCategory
    {
    }

    [FileExtension(".dll")]
    [Category("NatIVe")]
    public class DummyTestDiscovereWithArbitCasedCategory
    {
    }

    [FileExtension("csv")]
    [DefaultExecutorUri("csvexecutor")]
    public class DummyTestDiscovererWithOneFileExtensions
    {
    }

    [FileExtension("csv")]
    [FileExtension("docx")]
    [Category("managed")]
    [DefaultExecutorUri("csvexecutor")]
    public class DummyTestDiscovererWithTwoFileExtensions
    {
    }

    #endregion
}
