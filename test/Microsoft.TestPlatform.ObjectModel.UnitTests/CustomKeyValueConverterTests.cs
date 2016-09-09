﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CustomKeyValueConverterTests
    {
        private readonly CustomKeyValueConverter customKeyValueConverter;

        public CustomKeyValueConverterTests()
        {
            this.customKeyValueConverter = new CustomKeyValueConverter();
        }

        [TestMethod]
        public void CustomKeyValueConverterShouldDeserializeWellformedJson()
        {
            var json = "[{ \"Key\": \"key1\", \"Value\": \"val1\" }]";

            var data = this.customKeyValueConverter.ConvertFrom(null, CultureInfo.InvariantCulture, json) as KeyValuePair<string, string>[];

            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Length);
            Assert.AreEqual("key1", data[0].Key);
            Assert.AreEqual("val1", data[0].Value);
        }

        [TestMethod]
        public void CustomKeyValueConverterShouldDeserializeKeyValuePairArray()
        {
            var json = "[{ \"Key\": \"key1\", \"Value\": \"val1\" }, { \"Key\": \"key2\", \"Value\": \"val2\" }]";

            var data = this.customKeyValueConverter.ConvertFrom(null, CultureInfo.InvariantCulture, json) as KeyValuePair<string, string>[];

            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.Length);
            Assert.AreEqual("key1", data[0].Key);
            Assert.AreEqual("val1", data[0].Value);
            Assert.AreEqual("key2", data[1].Key);
            Assert.AreEqual("val2", data[1].Value);
        }

        [TestMethod]
        public void CustomKeyValueConverterShouldDeserializeEmptyArray()
        {
            var json = "[]";

            var data = this.customKeyValueConverter.ConvertFrom(null, CultureInfo.InvariantCulture, json) as KeyValuePair<string, string>[];

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Length);
        }

        [TestMethod]
        public void CustomKeyValueConverterShouldThrowOnDeserializeNullKeyOrValue()
        {
            var json = "[{ \"Key\": null, \"Value\": \"\" }]";

            var action = new Action(() => this.customKeyValueConverter.ConvertFrom(null, CultureInfo.InvariantCulture, json));

            Assert.ThrowsException<ArgumentNullException>(action);
        }

        [TestMethod]
        public void CustomKeyValueConverterShouldDeserializeEmptyKeyOrValue()
        {
            var json = "[{ \"Key\": \"\", \"Value\": \"\" }]";

            var data = this.customKeyValueConverter.ConvertFrom(null, CultureInfo.InvariantCulture, json) as KeyValuePair<string, string>[];

            Assert.AreEqual(1, data.Length);
            Assert.AreEqual(string.Empty, data[0].Key);
            Assert.AreEqual(string.Empty, data[0].Value);
        }
    }
}
