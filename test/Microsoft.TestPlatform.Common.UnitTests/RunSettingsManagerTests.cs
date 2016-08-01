// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunSettingsManagerTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            RunSettingsManager.Instance = null;
        }

        [TestMethod]
        public void InstanceShouldReturnARunSettingsManagerInstance()
        {
            var instance = RunSettingsManager.Instance;

            Assert.IsNotNull(instance);
            Assert.AreEqual(typeof(RunSettingsManager), instance.GetType());
        }

        [TestMethod]
        public void InstanceShouldReturnACachedValue()
        {
            var instance = RunSettingsManager.Instance;
            var instance2 = RunSettingsManager.Instance;

            Assert.AreEqual(instance, instance2);
        }

        [TestMethod]
        public void ActiveRunSettingsShouldBeNonNullByDefault()
        {
            var instance = RunSettingsManager.Instance;

            Assert.IsNotNull(instance.ActiveRunSettings);
        }

        [TestMethod]
        public void SetActiveRunSettingsShouldThrowIfRunSettingsPassedIsNull()
        {
            var instance = RunSettingsManager.Instance;

            Assert.ThrowsException<ArgumentNullException>(() => instance.SetActiveRunSettings(null));
        }

        [TestMethod]
        public void SetActiveRunSettingsShouldSetTheActiveRunSettingsProperty()
        {
            var instance = RunSettingsManager.Instance;

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml("<RunSettings></RunSettings>");

            instance.SetActiveRunSettings(runSettings);

            Assert.AreEqual(runSettings, instance.ActiveRunSettings);
        }
    }
}
