// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Helpers;

[TestClass]
public class PlatformFileHelperTests
{
    private string? _tempDirectory;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempDirectory is not null && Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [TestMethod]
    public void SetOwnerOnlyUnixDirectoryPermissionsDoesNotThrowWhenDirectoryDoesNotExist()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Should be a no-op, regardless of platform, when the directory was never created.
        PlatformFileHelper.SetOwnerOnlyUnixDirectoryPermissions(_tempDirectory);
    }

#if !NETFRAMEWORK
    [TestMethod]
    [OSCondition(OperatingSystems.Linux | OperatingSystems.OSX)]
    public void SetOwnerOnlyUnixDirectoryPermissionsRestrictsAccessToOwnerOnUnix()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        PlatformFileHelper.SetOwnerOnlyUnixDirectoryPermissions(_tempDirectory);

        var mode = File.GetUnixFileMode(_tempDirectory);
        Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, mode);
    }
#endif
}
