// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Globalization;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            ThrowsExceptionWithMessage<ArgumentException>(() =>
            {
                        new DataCollectorConfig(typeof(CustomDataCollectorWithoutUri));
                    },
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Resources.DataCollector_TypeIsNull,
                    typeof(CustomDataCollectorWithoutUri).FullName));
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfFriendlyNameIsEmpty()
        {
            ThrowsExceptionWithMessage<ArgumentException>(() =>
            {
                        new DataCollectorConfig(typeof(CustomDataCollectorWithEmptyFriendlyName));
                    },
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Resources.FriendlyNameIsNullOrEmpty,
                    typeof(CustomDataCollectorWithEmptyFriendlyName).FullName));
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfFriendlyNameIsNotSpecified()
        {
            ThrowsExceptionWithMessage<ArgumentException>(() =>
            {
                        new DataCollectorConfig(typeof(CustomDataCollectorWithoutFriendlyName));
                    },
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Resources.FriendlyNameIsNullOrEmpty,
                    typeof(CustomDataCollectorWithoutFriendlyName).FullName));
        }

        public static void ThrowsExceptionWithMessage<T>(Action action, string message) where T : Exception
        {
            var exception = Assert.ThrowsException<T>(action);
            StringAssert.Contains(exception.Message, message);
        }
    }
}
