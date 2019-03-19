// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SessionEventsTests
    {
        private SessionStartEventArgs sessionStartEventArgs;

        public SessionEventsTests()
        {
            this.sessionStartEventArgs = new SessionStartEventArgs();
        }

        [TestMethod]
        public void SessionStartEventArgsGetPropertiesShouldGetPropertiesEnumerator()
        {
            this.sessionStartEventArgs.SetPropertyValue("property1", 1);
            this.sessionStartEventArgs.SetPropertyValue("property2", 2);

            var properties = this.sessionStartEventArgs.GetProperties();
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
            this.sessionStartEventArgs.SetPropertyValue("property1", 1);
            this.sessionStartEventArgs.SetPropertyValue("property2", 2);

            var value = this.sessionStartEventArgs.GetPropertyValue<int>("property1");

            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void SessionStartEventArgsGetPropertyValueShouldGetPropertyValueInObject()
        {
            this.sessionStartEventArgs.SetPropertyValue("property1", 1);
            this.sessionStartEventArgs.SetPropertyValue("property2", 2);

            var value = this.sessionStartEventArgs.GetPropertyValue("property1");

            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void SessionStartEventArgsSetPropertyValueShouldCorrectlySetPropertyValue()
        {
            this.sessionStartEventArgs.SetPropertyValue("property1", 1);

            var properties = this.sessionStartEventArgs.GetProperties();
            int propertiesCount = 0;
            while (properties.MoveNext())
            {
                propertiesCount++;
            }

            var value = this.sessionStartEventArgs.GetPropertyValue<int>("property1");
            Assert.AreEqual(1, propertiesCount);
            Assert.AreEqual(1, value);
        }
    }
}
