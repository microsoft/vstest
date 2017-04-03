// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Globalization;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSTest.TestFramework.AssertExtensions;

    [TestClass]
    public class DataCollectorConfigTests
    {
        [TestMethod]
        public void ConstructorShouldSetCorrectFriendlyNameAndUri()
        {
            var dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector));

            Assert.AreEqual("CustomDataCollector", dataCollectorConfig.FriendlyName);
            Assert.AreEqual("my://custom/datacollector", dataCollectorConfig.TypeUri.ToString());
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfTypeIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        new DataCollectorConfig(null);
                    });
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfUriIsNotSpecifiedInDataCollector()
        {
            Assert.That.Throws<ArgumentException>(() =>
                    {
                        new DataCollectorConfig(typeof(CustomDataCollectorWithoutUri));
                    })
                    .WithMessage(string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.Resources.DataCollector_TypeIsNull,
                            typeof(CustomDataCollectorWithoutUri).FullName));
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfFriendlyNameIsEmpty()
        {
            Assert.That.Throws<ArgumentException>(() =>
                    {
                        new DataCollectorConfig(typeof(CustomDataCollectorWithEmptyFriendlyName));
                    })
                    .WithMessage(string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.Resources.FriendlyNameIsNullOrEmpty,
                            typeof(CustomDataCollectorWithEmptyFriendlyName).FullName));
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfFriendlyNameIsNotSpecified()
        {
            Assert.That.Throws<ArgumentException>(() =>
                    {
                        new DataCollectorConfig(typeof(CustomDataCollectorWithoutFriendlyName));
                    }).
                    WithMessage(string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.Resources.FriendlyNameIsNullOrEmpty,
                            typeof(CustomDataCollectorWithoutFriendlyName).FullName));
        }
    }
}
