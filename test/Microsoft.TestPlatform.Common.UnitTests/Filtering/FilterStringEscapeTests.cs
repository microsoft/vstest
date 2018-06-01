// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering
{
    using System;
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Resources;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FilterStringEscapeTests
    {
        [TestMethod]
        public void EscapeNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => FilterHelpers.Escape(null));   
            Assert.ThrowsException<ArgumentNullException>(() => FilterHelpers.Unescape(null));
        }

        [TestMethod]
        public void EscapeEmptyString()
        {
            Assert.AreEqual(string.Empty, FilterHelpers.Escape(string.Empty)); 
            Assert.AreEqual(string.Empty, FilterHelpers.Unescape(string.Empty));
        }

        [TestMethod]
        public void EscapeStringWithoutSpecialCharacters()
        {
            var str = "TestNamespace.TestClass.TestMethod";
            Assert.AreEqual(str, FilterHelpers.Escape(str));
            Assert.AreEqual(str, FilterHelpers.Unescape(str));
        }

        [TestMethod]
        public void EscapeStringWithParaenthesis()
        {
            var value = "TestClass(1).TestMethod(2)";
            var escapedValue = FilterHelpers.Escape(value);

            Assert.AreEqual(@"TestClass%11%2.TestMethod%12%2", escapedValue);
            Assert.AreEqual(value, FilterHelpers.Unescape(escapedValue));
        }

        [TestMethod]
        public void EscapeStringWithSpecialCharacters()
        {
            var value = "TestClass(\"a | b\").TestMethod(\"x != y\")";
            var escapedValue = FilterHelpers.Escape(value);

            Assert.AreEqual("TestClass%1\"a %4 b\"%2.TestMethod%1\"x %6%5 y\"%2", escapedValue);
            Assert.AreEqual(value, FilterHelpers.Unescape(escapedValue));
        }

        [TestMethod]
        public void EscapeStringWithPrefix()
        {
            var value = "printf(\"%s | %d\", text, 0)";
            var escapedValue = FilterHelpers.Escape(value);

            Assert.AreEqual("printf%1\"%0s %4 %0d\", text, 0%2", escapedValue);
            Assert.AreEqual(value, FilterHelpers.Unescape(escapedValue));
        }

        [TestMethod]
        public void UnescapeInvalid1()
        {
            var invalidString = @"TestClass%8""a %4 b""%2.TestMethod";     
            Assert.ThrowsException<ArgumentException>(() => FilterHelpers.Unescape(invalidString), string.Format(CultureInfo.CurrentCulture, Resources.TestCaseFilterEscapeException, invalidString));
        }

        [TestMethod]
        public void UnescapeInvalid2()
        {
            var invalidString = @"TestClass%";
            Assert.ThrowsException<ArgumentException>(() => FilterHelpers.Unescape(invalidString), string.Format(CultureInfo.CurrentCulture, Resources.TestCaseFilterEscapeException, invalidString));
        }
    }
}
