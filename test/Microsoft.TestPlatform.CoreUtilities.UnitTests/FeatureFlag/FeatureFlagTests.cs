// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.FeatureFlag;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.Utilities;

[TestClass]
public class FeatureFlagTests
{
    [TestMethod]
    public void SingletonAlwaysReturnsTheSameInstance()
    {
        Assert.IsTrue(ReferenceEquals(FeatureFlag.Instance, FeatureFlag.Instance));
    }
}
