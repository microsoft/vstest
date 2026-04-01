// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection.Metadata;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Regression tests for Issue #1908 / PR #3454:
/// PortablePdbReader did not support embedded PDBs (inside DLLs), only external PDB files.
/// Fix: Added constructor that accepts MetadataReaderProvider directly.
/// </summary>
[TestClass]
public class ICSRegressionTests
{
    #region Issue #1908 - Support embedded PDBs via MetadataReaderProvider constructor

    [TestMethod]
    public void Constructor_WithNullMetadataReaderProvider_ShouldThrowArgumentNullException()
    {
        // The MetadataReaderProvider constructor validates its argument,
        // throwing ArgumentNullException when null is passed.
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new PortablePdbReader((MetadataReaderProvider)null!));
    }

    [TestMethod]
    public void Constructor_MetadataReaderProviderOverload_ShouldExist()
    {
        // Verify the constructor accepting MetadataReaderProvider exists.
        // This constructor was added to support embedded PDBs (Issue #1908).
        var ctorInfo = typeof(PortablePdbReader).GetConstructor(
            new[] { typeof(MetadataReaderProvider) });

        Assert.IsNotNull(ctorInfo,
            "PortablePdbReader should have a constructor accepting MetadataReaderProvider " +
            "to support embedded PDBs (Issue #1908).");
    }

    [TestMethod]
    public void Constructor_StreamOverload_ShouldAlsoExist()
    {
        // Ensure the original Stream constructor still exists alongside the new one.
        var ctorInfo = typeof(PortablePdbReader).GetConstructor(
            new[] { typeof(System.IO.Stream) });

        Assert.IsNotNull(ctorInfo,
            "PortablePdbReader should retain its original Stream constructor.");
    }

    #endregion
}
