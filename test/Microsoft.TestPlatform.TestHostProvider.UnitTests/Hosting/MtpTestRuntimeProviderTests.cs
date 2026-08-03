// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace TestPlatform.TestHostProvider.UnitTests.Hosting;

[TestClass]
public class MtpTestRuntimeProviderTests
{
    private readonly Mock<IFeatureFlag> _featureFlag = new();

    [TestMethod]
    public void CanExecuteCurrentRunConfigurationReturnsFalseWhenMtpOptInIsNotSet()
    {
        _featureFlag.Setup(featureFlag => featureFlag.IsSet(FeatureFlag.VSTEST_OPTIN_MTP)).Returns(false);
        var detectorWasCalled = false;
        var provider = new MtpTestRuntimeProvider(
            _featureFlag.Object,
            _ =>
            {
                detectorWasCalled = true;
                return true;
            });

        var canExecute = provider.CanExecuteCurrentRunConfiguration(["mtp.dll"]);

        Assert.IsFalse(canExecute);
        Assert.IsFalse(detectorWasCalled);
        _featureFlag.Verify(featureFlag => featureFlag.IsSet(FeatureFlag.VSTEST_OPTIN_MTP), Times.Once);
    }

    [TestMethod]
    public void CanExecuteCurrentRunConfigurationReturnsTrueWhenMtpOptInIsSetAndAllSourcesAreMtp()
    {
        _featureFlag.Setup(featureFlag => featureFlag.IsSet(FeatureFlag.VSTEST_OPTIN_MTP)).Returns(true);
        var provider = new MtpTestRuntimeProvider(_featureFlag.Object, _ => true);

        var canExecute = provider.CanExecuteCurrentRunConfiguration(["first.dll", "second.dll"]);

        Assert.IsTrue(canExecute);
        _featureFlag.Verify(featureFlag => featureFlag.IsSet(FeatureFlag.VSTEST_OPTIN_MTP), Times.Once);
    }

    [TestMethod]
    public void CanExecuteCurrentRunConfigurationReturnsFalseWhenMtpOptInIsSetAndAnySourceIsNotMtp()
    {
        _featureFlag.Setup(featureFlag => featureFlag.IsSet(FeatureFlag.VSTEST_OPTIN_MTP)).Returns(true);
        var provider = new MtpTestRuntimeProvider(_featureFlag.Object, source => source == "mtp.dll");

        var canExecute = provider.CanExecuteCurrentRunConfiguration(["mtp.dll", "classic.dll"]);

        Assert.IsFalse(canExecute);
        _featureFlag.Verify(featureFlag => featureFlag.IsSet(FeatureFlag.VSTEST_OPTIN_MTP), Times.Once);
    }
}
