// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;

    [TestClass]
    public class SessionEventsTests
    {
        private readonly SessionStartEventArgs sessionStartEventArgs;

        public SessionEventsTests()
        {
            var properties = new Dictionary<string, object>
            {
                { "property1", 1 },
                { "property2", 2 }
            };

            sessionStartEventArgs = new SessionStartEventArgs(properties);
        }

        [TestMethod]
        public void SessionStartEventArgsGetPropertiesShouldGetPropertiesEnumerator()
        {
            var properties = sessionStartEventArgs.GetProperties();
            int propertiesCount = 0;
            while (properties.MoveNext())
            {
                propertiesCount++;
            }

            Assert.AreEqual(2, propertiesCount);
        }

        [TestMethod]
        public void SessionStartEventArgsGetPropertyValueShouldGetPropertyValue()
        {
            var value = sessionStartEventArgs.GetPropertyValue<int>("property1");

            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void SessionStartEventArgsGetPropertyValueShouldGetPropertyValueInObject()
        {
            var value = sessionStartEventArgs.GetPropertyValue("property1");

            Assert.AreEqual(1, value);
        }
    }
}
