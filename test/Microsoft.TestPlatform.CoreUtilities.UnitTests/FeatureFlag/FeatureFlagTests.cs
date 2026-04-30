// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests;

[TestClass]
public class FeatureFlagTests
{
    [TestMethod]
    public void SingletonAlwaysReturnsTheSameInstance()
    {
        Assert.IsTrue(ReferenceEquals(FeatureFlag.Instance, FeatureFlag.Instance));
    }

    [TestMethod]
    public void IsSetShouldReturnFalseWhenEnvVarIsSetToZero()
    {
        // Use a unique name that has never been seen by this singleton instance.
        const string flagName = "TEST_FEATUREFLAG_ISSET_ZERO_9A1B2C";
        Environment.SetEnvironmentVariable(flagName, "0");
        try
        {
            Assert.IsFalse(FeatureFlag.Instance.IsSet(flagName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(flagName, null);
        }
    }

    [TestMethod]
    public void IsSetShouldReturnFalseWhenEnvVarIsNotSet()
    {
        const string flagName = "TEST_FEATUREFLAG_ISSET_NULL_9A1B2C";
        Environment.SetEnvironmentVariable(flagName, null);
        Assert.IsFalse(FeatureFlag.Instance.IsSet(flagName));
    }

    [TestMethod]
    public void IsSetShouldReturnTrueWhenEnvVarIsSetToOne()
    {
        const string flagName = "TEST_FEATUREFLAG_ISSET_ONE_9A1B2C";
        Environment.SetEnvironmentVariable(flagName, "1");
        try
        {
            Assert.IsTrue(FeatureFlag.Instance.IsSet(flagName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(flagName, null);
        }
    }

    [TestMethod]
    public void IsSetShouldReturnTrueWhenEnvVarIsAnyNonZeroString()
    {
        const string flagName = "TEST_FEATUREFLAG_ISSET_NONZERO_9A1B2C";
        Environment.SetEnvironmentVariable(flagName, "true");
        try
        {
            Assert.IsTrue(FeatureFlag.Instance.IsSet(flagName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(flagName, null);
        }
    }

    [TestMethod]
    public void IsSetShouldCacheResultSoSubsequentEnvVarChangesAreIgnored()
    {
        const string flagName = "TEST_FEATUREFLAG_ISSET_CACHE_9A1B2C";
        Environment.SetEnvironmentVariable(flagName, "1");
        try
        {
            bool firstCheck = FeatureFlag.Instance.IsSet(flagName);
            // Clear the env var after the first call; the cached value should persist.
            Environment.SetEnvironmentVariable(flagName, null);
            bool secondCheck = FeatureFlag.Instance.IsSet(flagName);

            Assert.IsTrue(firstCheck);
            Assert.IsTrue(secondCheck, "IsSet should return the cached value, not re-read the env var.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(flagName, null);
        }
    }
}
