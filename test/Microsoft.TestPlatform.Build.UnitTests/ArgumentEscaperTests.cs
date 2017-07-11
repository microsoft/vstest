// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Build.Utils.UnitTests
{
    using Microsoft.TestPlatform.Build.Utils;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ArgumentEscaperTests
    {
        [TestMethod]
        public void EscapeArgForProcessStartShouldAddDoubleQuoteIfThereIsSpace()
        {
            string stringWithDoubleQuote = "Some string";

            string expected = "\"Some string\"";
            string result = ArgumentEscaper.EscapeArgForProcessStart(stringWithDoubleQuote);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void EscapeArgForProcessStartShouldPreserveDoubleQuote()
        {
            string stringWithDoubleQuote = "Some\"string";

            string expected = "Some\\\"string";
            string result = ArgumentEscaper.EscapeArgForProcessStart(stringWithDoubleQuote);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void EscapeArgForProcessStartShouldPreserveBackSlash()
        {
            string stringWithDoubleQuote = @"Some\\string";

            string expected = "Some\\\\string";
            string result = ArgumentEscaper.EscapeArgForProcessStart(stringWithDoubleQuote);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void EscapeArgForProcessStartShouldPreserveBackSlashIfStringHasWhiteSpace()
        {
            string stringWithDoubleQuote = @"Some string With Space\\";

            string expected = @"""Some string With Space\\\\""";
            string result = ArgumentEscaper.EscapeArgForProcessStart(stringWithDoubleQuote);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ShouldSurroundWithQuotesShouldReturnFalseIfAlreadySurroundWithQuotes()
        {
            string stringSurroundWithQuotes = "\"" + "some string" + "\"";

            Assert.IsFalse(ArgumentEscaper.ShouldSurroundWithQuotes(stringSurroundWithQuotes));
        }

        [TestMethod]
        public void ShouldSurroundWithQuotesShouldReturnFalseIfItIsNotSurroundWithQuotesAndHasNoWhiteSpace()
        {
            string stringWithoutSpace = "someStringWithNoWhiteSpace";

            Assert.IsFalse(ArgumentEscaper.ShouldSurroundWithQuotes(stringWithoutSpace));
        }

        [TestMethod]
        public void ShouldSurroundWithQuotesShouldReturnTrueIfItIsNotSurroundWithQuotesAndHasWhiteSpace()
        {
            string stringWithSpace = "some String With WhiteSpace";

            Assert.IsTrue(ArgumentEscaper.ShouldSurroundWithQuotes(stringWithSpace));
        }

        [TestMethod]
        public void IsSurroundedWithQuotesShouldReturnTrueIfStringIsSurrondedByQuotes()
        {
            string stringSurroundWithQuotes = "\"" + "some string" + "\"";

            Assert.IsTrue(ArgumentEscaper.IsSurroundedWithQuotes(stringSurroundWithQuotes));
        }

        [TestMethod]
        public void IsSurroundedWithQuotesShouldReturnFalseIfStringIsNotSurrondedByQuotes()
        {
            string stringNotSurroundWithQuotes = "some string";

            Assert.IsFalse(ArgumentEscaper.IsSurroundedWithQuotes(stringNotSurroundWithQuotes));
        }
    }
}
