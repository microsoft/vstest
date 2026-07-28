// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests;

[TestClass]
public class XmlPersistenceTests
{
    [TestMethod]
    public void SaveObjectShouldReplaceInvalidCharacter()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        // we are handling only #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] plus
        // well-formed surrogate pairs, which encode [#x10000-#x10FFFF].
        char[] invalidXmlCharacterArray = new char[5];
        invalidXmlCharacterArray[0] = (char)0x5;
        invalidXmlCharacterArray[1] = (char)0xb;
        invalidXmlCharacterArray[2] = (char)0xf;
        invalidXmlCharacterArray[3] = (char)0xfffe;
        invalidXmlCharacterArray[4] = (char)0x0;

        string strWithInvalidCharForXml = new(invalidXmlCharacterArray);
        XmlPersistence.SaveObject(strWithInvalidCharForXml, node, null, "dummy");

        string expectedResult = "\\u0005\\u000b\\u000f\\ufffe\\u0000";
        Assert.AreEqual(0, string.Compare(expectedResult, node.InnerXml));
    }

    [TestMethod]
    public void SaveObjectShouldNotReplaceWellFormedSurrogatePairInElementText()
    {
        // A surrogate pair encodes a character in [#x10000-#x10FFFF], which the XML spec
        // lists as valid, so it must survive untouched. Here U+1F389 (🎉).
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        XmlPersistence.SaveObject("party \U0001F389 done", node, null, "dummy");

        Assert.AreEqual("party \U0001F389 done", node.InnerXml);
    }

    [TestMethod]
    public void SaveObjectShouldNotReplaceWellFormedSurrogatePairInAttributeValue()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        xmlPersistence.SaveObject("party \U0001F389 done", node, "@testName", null);

        Assert.AreEqual("party \U0001F389 done", node.GetAttribute("testName"));
    }

    [TestMethod]
    public void SaveObjectShouldReplaceLoneHighSurrogate()
    {
        // Unlike a pair, a lone high surrogate is not a valid Unicode scalar value and is
        // not valid XML (XmlWriter would throw on it), so it must still be escaped.
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        XmlPersistence.SaveObject("a" + (char)0xd800 + "b", node, null, "dummy");

        Assert.AreEqual("a\\ud800b", node.InnerXml);
    }

    [TestMethod]
    public void SaveObjectShouldReplaceLoneLowSurrogate()
    {
        // A low surrogate that is not preceded by a high surrogate is equally invalid.
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        XmlPersistence.SaveObject((char)0xdc00 + "b", node, null, "dummy");

        Assert.AreEqual("\\udc00b", node.InnerXml);
    }

    [TestMethod]
    public void SaveObjectShouldReplaceHighSurrogateAtEndOfString()
    {
        // The pair-matching logic must not read past the end of the string.
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        XmlPersistence.SaveObject("ab" + (char)0xd800, node, null, "dummy");

        Assert.AreEqual("ab\\ud800", node.InnerXml);
    }

    [TestMethod]
    public void SaveObjectShouldHandleMixOfValidPairLoneSurrogateAndInvalidCharacters()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        XmlPersistence.SaveObject("\U0001F389" + (char)0xd800 + "\v" + (char)0xdc00, node, null, "dummy");

        // The pair is preserved; the lone high surrogate, the vertical tab and the
        // trailing lone low surrogate are all escaped.
        Assert.AreEqual("\U0001F389\\ud800\\u000b\\udc00", node.InnerXml);
    }

    [TestMethod]
    public void SaveObjectShouldNotReplaceNonAsciiBmpCharacters()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        XmlPersistence.SaveObject("Grüße 日本語 Čau", node, null, "dummy");

        Assert.AreEqual("Grüße 日本語 Čau", node.InnerXml);
    }

    [TestMethod]
    public void SaveObjectShouldProduceWellFormedXmlWithAstralCharacters()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        xmlPersistence.SaveObject("party \U0001F389 done", node, "@testName", null);
        XmlPersistence.SaveObject("output \U0001F389 here", node, null, "dummy");

        XmlDocument document = new();
        document.LoadXml(node.OuterXml);

        Assert.AreEqual("party \U0001F389 done", document.DocumentElement!.GetAttribute("testName"));
        Assert.AreEqual("output \U0001F389 here", document.DocumentElement.InnerText);
    }

    [TestMethod]
    public void SaveObjectShouldNotReplaceValidCharacter()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");

        // we are handling only #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
        char[] validXmlCharacterArray = new char[8];
        validXmlCharacterArray[0] = (char)0x9;
        validXmlCharacterArray[1] = (char)0xa;
        validXmlCharacterArray[2] = (char)0xd;
        validXmlCharacterArray[3] = (char)0x20;
        validXmlCharacterArray[4] = (char)0xc123;
        validXmlCharacterArray[5] = (char)0xe000;
        validXmlCharacterArray[6] = (char)0xea12;
        validXmlCharacterArray[7] = (char)0xfffd;

        string strWithValidCharForXml = new(validXmlCharacterArray);

        XmlPersistence.SaveObject(strWithValidCharForXml, node, null, "dummy");

        string expectedResult = "\t\n\r 섣�";
        Assert.AreEqual(0, string.Compare(expectedResult, node.InnerXml));
    }

    [TestMethod]
    public void SaveObjectShouldReplaceOnlyInvalidCharacter()
    {
        XmlPersistence xmlPersistence = new();
        var node = xmlPersistence.CreateRootElement("TestRun");
        string strWithInvalidCharForXml = "This string has these \0 \v invalid characters";
        XmlPersistence.SaveObject(strWithInvalidCharForXml, node, null, "dummy");
        string expectedResult = "This string has these \\u0000 \\u000b invalid characters";
        Assert.AreEqual(0, string.Compare(expectedResult, node.InnerXml));
    }
}
