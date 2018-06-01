// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Resources;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FilterHelpersTests
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

            Assert.AreEqual(@"TestClass\(1\).TestMethod\(2\)", escapedValue);
            Assert.AreEqual(value, FilterHelpers.Unescape(escapedValue));
        }

        [TestMethod]
        public void EscapeStringWithSpecialCharacters()
        {
            var value = @"TestClass(""a | b"").TestMethod(""x != y"")";
            var escapedValue = @"TestClass\(""a \| b""\).TestMethod\(""x \!\= y""\)";

            Assert.AreEqual(escapedValue, FilterHelpers.Escape(value));
            Assert.AreEqual(value, FilterHelpers.Unescape(escapedValue));
        }

        [TestMethod]
        public void EscapeStringWithPrefix()
        {
            var value = @"printf(""\r\n"")";
            var escapedValue = @"printf\(""\\r\\n""\)";

            Assert.AreEqual(escapedValue, FilterHelpers.Escape(value));
            Assert.AreEqual(value, FilterHelpers.Unescape(escapedValue));
        }

        [TestMethod]
        public void UnescapeInvalid1()
        {
            var invalidString = @"TestClass\$""a %4 b""%2.TestMethod";     
            Assert.ThrowsException<ArgumentException>(() => FilterHelpers.Unescape(invalidString), string.Format(CultureInfo.CurrentCulture, Resources.TestCaseFilterEscapeException, invalidString));
        }

        [TestMethod]
        public void UnescapeInvalid2()
        {
            var invalidString = @"TestClass\";
            Assert.ThrowsException<ArgumentException>(() => FilterHelpers.Unescape(invalidString), string.Format(CultureInfo.CurrentCulture, Resources.TestCaseFilterEscapeException, invalidString));
        }

        [TestMethod]
        public void TokenizeConditionShouldHandleEscapedBang()
        {                                               
            var conditionString = @"FullyQualifiedName=TestMethod\(""\!""\)" ;

            var tokens = FilterHelpers.TokenizeFilterConditionString(conditionString).ToArray();

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual("FullyQualifiedName", tokens[0]);
            Assert.AreEqual("=", tokens[1]);
            Assert.AreEqual(@"TestMethod\(""\!""\)", tokens[2]);
        }

        [TestMethod]
        public void TokenizeConditionShouldHandleEscapedNotEqual1()
        {
            var conditionString = @"FullyQualifiedName=TestMethod\(""\!\=""\)";

            var tokens = FilterHelpers.TokenizeFilterConditionString(conditionString).ToArray();

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual("FullyQualifiedName", tokens[0]);
            Assert.AreEqual("=", tokens[1]);
            Assert.AreEqual(@"TestMethod\(""\!\=""\)", tokens[2]);
        }

        [TestMethod]
        public void TokenizeConditionShouldHandleEscapedNotEqual2()
        {
            var conditionString = @"FullyQualifiedName!=TestMethod\(""\!\=""\)";

            var tokens = FilterHelpers.TokenizeFilterConditionString(conditionString).ToArray();

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual("FullyQualifiedName", tokens[0]);
            Assert.AreEqual("!=", tokens[1]);
            Assert.AreEqual(@"TestMethod\(""\!\=""\)", tokens[2]);
        }

        [TestMethod]
        public void TokenizeConditionShouldHandleEscapedBackslash()
        {                                                           
            var conditionString = @"FullyQualifiedName=TestMethod\(""\\""\)";

            var tokens = FilterHelpers.TokenizeFilterConditionString(conditionString).ToArray();

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual("FullyQualifiedName", tokens[0]);
            Assert.AreEqual("=", tokens[1]);
            Assert.AreEqual(@"TestMethod\(""\\""\)", tokens[2]);
        }

        [TestMethod]
        public void TokenizeConditionShouldHandleEscapedTilde()
        {                                               
            var conditionString = @"FullyQualifiedName~TestMethod\(""\~""\)";

            var tokens = FilterHelpers.TokenizeFilterConditionString(conditionString).ToArray();

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual("FullyQualifiedName", tokens[0]);
            Assert.AreEqual("~", tokens[1]);
            Assert.AreEqual(@"TestMethod\(""\~""\)", tokens[2]);
        }

        [TestMethod]
        public void TokenizeConditionShouldHandleSingleUnescapedBang()
        {                                                                
            var conditionString = @"FullyQualifiedName!=TestMethod\(""!""\)";

            var tokens = FilterHelpers.TokenizeFilterConditionString(conditionString).ToArray();

            Assert.AreEqual(5, tokens.Length);
            Assert.AreEqual("FullyQualifiedName", tokens[0]);
            Assert.AreEqual("!=", tokens[1]);
            Assert.AreEqual(@"TestMethod\(""", tokens[2]);
            Assert.AreEqual("!", tokens[3]);                
            Assert.AreEqual(@"""\)", tokens[4]);
        }

        [TestMethod]
        public void TokenizeFilterShouldHandleEscapedParenthesis()
        {
            var conditionString = @"(T1\(\) | T2\(\))";

            var tokens = FilterHelpers.TokenizeFilterExpressionString(conditionString).ToArray();

            Assert.AreEqual(5, tokens.Length);
            Assert.AreEqual("(", tokens[0]);
            Assert.AreEqual(@"T1\(\) ", tokens[1]);
            Assert.AreEqual(@"|", tokens[2]);
            Assert.AreEqual(@" T2\(\)", tokens[3]);
            Assert.AreEqual(")", tokens[4]);
        }

        [TestMethod]
        public void TokenizeFilterShouldHandleEmptyParenthesis()
        {
            var conditionString = @"  (  )  ";

            var tokens = FilterHelpers.TokenizeFilterExpressionString(conditionString).ToArray();

            Assert.AreEqual(5, tokens.Length);
            Assert.AreEqual("  ", tokens[0]);
            Assert.AreEqual("(", tokens[1]);
            Assert.AreEqual("  ", tokens[2]);
            Assert.AreEqual(")", tokens[3]);
            Assert.AreEqual("  ", tokens[4]);
        }

        [TestMethod]
        public void TokenizeFilterShouldHandleEscapedBackslash()
        {
            var conditionString = @"(FQN!=T1\(""\\""\) | FQN!=T2\(\))";

            var tokens = FilterHelpers.TokenizeFilterExpressionString(conditionString).ToArray();

            Assert.AreEqual(5, tokens.Length);
            Assert.AreEqual("(", tokens[0]);
            Assert.AreEqual(@"FQN!=T1\(""\\""\) ", tokens[1]);
            Assert.AreEqual(@"|", tokens[2]);
            Assert.AreEqual(@" FQN!=T2\(\)", tokens[3]);
            Assert.AreEqual(")", tokens[4]);
        }

        [TestMethod]
        public void TokenizeFilterShouldHandleNestedParenthesis()
        {
            var conditionString = @"((FQN!=T1|FQN!=T2)&(Category=Foo\(\)))";

            var tokens = FilterHelpers.TokenizeFilterExpressionString(conditionString).ToArray();

            Assert.AreEqual(11, tokens.Length);
            Assert.AreEqual("(", tokens[0]);
            Assert.AreEqual("(", tokens[1]);
            Assert.AreEqual(@"FQN!=T1", tokens[2]);
            Assert.AreEqual(@"|", tokens[3]);
            Assert.AreEqual(@"FQN!=T2", tokens[4]);
            Assert.AreEqual(")", tokens[5]);
            Assert.AreEqual("&", tokens[6]);
            Assert.AreEqual("(", tokens[7]);
            Assert.AreEqual(@"Category=Foo\(\)", tokens[8]);
            Assert.AreEqual(")", tokens[9]);
            Assert.AreEqual(")", tokens[10]);
        }

        [TestMethod]
        public void TokenizeFilterShouldHandleInvalidEscapeSequence()
        {
            var conditionString = @"(T1\#\#)|T2\)";

            var tokens = FilterHelpers.TokenizeFilterExpressionString(conditionString).ToArray();

            Assert.AreEqual(5, tokens.Length);
            Assert.AreEqual("(", tokens[0]);
            Assert.AreEqual(@"T1\#\#", tokens[1]);
            Assert.AreEqual(@")", tokens[2]);
            Assert.AreEqual(@"|", tokens[3]);
            Assert.AreEqual(@"T2\)", tokens[4]);
        }
    }
}
