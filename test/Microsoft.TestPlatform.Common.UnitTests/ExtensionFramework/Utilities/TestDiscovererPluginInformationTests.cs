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
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.AreEqual(typeof(DummyTestDiscovererWithNoFileExtensions).AssemblyQualifiedName, testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnTestExtensionTypesName()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.AreEqual(typeof(DummyTestDiscovererWithNoFileExtensions).AssemblyQualifiedName, testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void FileExtensionsShouldReturnEmptyListIfADiscovererSupportsNoFileExtensions()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.IsNotNull(testPluginInformation.FileExtensions);
            Assert.AreEqual(0, testPluginInformation.FileExtensions.Count);
        }

        [TestMethod]
        public void FileExtensionsShouldReturnAFileExtensionForADiscoverer()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithOneFileExtensions));
            CollectionAssert.AreEqual(new List<string> { "csv"}, testPluginInformation.FileExtensions);
        }

        [TestMethod]
        public void FileExtensionsShouldReturnSupportedFileExtensionsForADiscoverer()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithTwoFileExtensions));
            CollectionAssert.AreEqual(new List<string> {"csv", "docx"}, testPluginInformation.FileExtensions);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasNoCategory()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithNoCategory));
            Assert.AreEqual(AssemblyType.None, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasCategoryWithNoValue()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithCategoryHavingNoValue));
            Assert.AreEqual(AssemblyType.None, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasCategoryWithEmptyValue()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithCategoryHavingEmptyValue));
            Assert.AreEqual(AssemblyType.None, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNativeIfDiscovererHasNativeCategory()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithNativeCategory));
            Assert.AreEqual(AssemblyType.Native, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnManagedIfDiscovererHasManagedCategory()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithManagedCategory));
            Assert.AreEqual(AssemblyType.Managed, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnNoneIfDiscovererHasUnknownCategory()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithUnknownCategory));
            Assert.AreEqual(AssemblyType.None, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void AssemblyTypeShouldReturnAssemblyTypeIfDiscovererHasCategoryInArbitCasing()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovereWithArbitCasedCategory));
            Assert.AreEqual(AssemblyType.Native, testPluginInformation.AssemblyType);
        }

        [TestMethod]
        public void DefaultExecutorUriShouldReturnEmptyListIfADiscovererDoesNotHaveOne()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithNoFileExtensions));
            Assert.IsNotNull(testPluginInformation.DefaultExecutorUri);
            Assert.AreEqual(string.Empty, testPluginInformation.DefaultExecutorUri);
        }

        [TestMethod]
        public void DefaultExecutorUriShouldReturnDefaultExecutorUriOfADiscoverer()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithOneFileExtensions));
            Assert.AreEqual("csvexecutor", testPluginInformation.DefaultExecutorUri);
        }

        [TestMethod]
        public void MetadataShouldReturnFileExtensionsAndDefaultExecutorUriAndAssemblyType()
        {
            testPluginInformation = new TestDiscovererPluginInformation(typeof(DummyTestDiscovererWithTwoFileExtensions));

            var expectedFileExtensions = new List<string> { "csv", "docx" };
            var testPluginMetada = testPluginInformation.Metadata.ToArray();

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
