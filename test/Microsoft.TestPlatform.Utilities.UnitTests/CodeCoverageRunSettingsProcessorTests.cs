﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CodeCoverageRunSettingsProcessorTests
    {
        #region Members
        private XmlElement defaultSettings;

        private CodeCoverageRunSettingsProcessor processor;
        #endregion

        #region Constructors
        public CodeCoverageRunSettingsProcessorTests()
        {
            this.defaultSettings = this.GetDefaultConfiguration();
            this.processor = new CodeCoverageRunSettingsProcessor(this.defaultSettings);
        }
        #endregion

        #region Test Methods
        [TestMethod]
        public void ProcessingShouldReturnNullForNullOrEmptySettings()
        {
            Assert.IsNull(processor.Process((string)null));
            Assert.IsNull(processor.Process(""));

            Assert.IsNull(processor.Process((XmlNode)null));

            Assert.IsNull(processor.Process((XmlDocument)null));
        }

        [TestMethod]
        public void MissingCodeCoverageTagShouldAddDefaultTag()
        {
            const string settings = "<Configuration></Configuration>";
            string expected = $"{this.defaultSettings.OuterXml}";

            Assert.AreEqual(expected, processor.Process(settings).OuterXml);
        }

        [TestMethod]
        public void EmptyCodeCoverageTagShouldAddDefaultTag()
        {
            const string settings = "<Configuration><CodeCoverage></CodeCoverage></Configuration>";
            var processedNode = processor.Process(settings);
            Assert.IsNotNull(processedNode);

            var codeCoverageNodes = this.ExtractNodes(processedNode, this.defaultSettings, "./CodeCoverage");

            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./ModulePaths/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Functions/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Attributes/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Sources/Exclude");
        }

        [TestMethod]
        public void MergeDefaultsDisabledShouldReturnInputUnaltered()
        {
            var settings = string.Join(
                Environment.NewLine,
                @"<Configuration>",
                @"  <CodeCoverage>",
                @"    <ModulePaths mergeDefaults=""false""></ModulePaths>",
                @"    <Functions mergeDefaults=""false"">",
                @"      <Exclude></Exclude>",
                @"    </Functions>",
                @"    <Sources mergeDefaults=""false"">",
                @"      <Exclude>",
                @"          <Source>.*\\atlmfc\\.*</Source>",
                @"          <Source>.*\\atlmbfc\\.*</Source>",
                @"          <Source>.*\\vctools\\.*</Source>",
                @"          <Source>.*\\public\\sdk2\\.*</Source>",
                @"          <Source>.*\\externalapis\\.*</Source>",
                @"          <Source>.*\\microsoft sdks\\.*</Source>",
                @"          <Source>.*\\vc\\include\\.*</Source>",
                @"          <Source>.*\\msclr\\.*</Source>",
                @"          <Source>.*\\ucrt\\.*</Source>",
                @"      </Exclude>",
                @"    </Sources>",
                @"    <Attributes mergeDefaults=""false""></Attributes>",
                @"  </CodeCoverage>",
                @"</Configuration>");

            var document = new XmlDocument();
            document.LoadXml(settings);

            Assert.AreEqual(document.OuterXml, processor.Process(settings).OuterXml);
        }

        [TestMethod]
        public void MixedTestShouldCorrectlyAddMissingTags()
        {
            var settings = string.Join(
                Environment.NewLine,
                @"<Configuration>",
                @"  <CodeCoverage>",
                @"    <ModulePaths></ModulePaths>",
                @"    <Functions>",
                @"      <Exclude></Exclude>",
                @"    </Functions>",
                @"    <Sources>",
                @"      <Exclude>",
                @"          <Source>.*\\atlmfc\\.*</Source>",
                @"          <Source>.*\\atlmbfc\\.*</Source>",
                @"          <Source>.*\\vctools\\.*</Source>",
                @"          <Source>.*\\public\\sdk2\\.*</Source>",
                @"          <Source>.*\\externalapis\\.*</Source>",
                @"          <Source>.*\\microsoft sdks\\.*</Source>",
                @"          <Source>.*\\vc\\include\\.*</Source>",
                @"          <Source>.*\\msclr\\.*</Source>",
                @"          <Source>.*\\ucrt\\.*</Source>",
                @"      </Exclude>",
                @"    </Sources>",
                @"  </CodeCoverage>",
                @"</Configuration>");

            var expectedResult = string.Join(
                Environment.NewLine,
                @"<Configuration>",
                @"  <CodeCoverage>",
                @"    <ModulePaths>",
                @"      <Exclude>",
                @"         <ModulePath>.*CPPUnitTestFramework.*</ModulePath>",
                @"         <ModulePath>.*vstest.console.*</ModulePath>",
                @"         <ModulePath>.*microsoft.intellitrace.*</ModulePath>",
                @"         <ModulePath>.*testhost.*</ModulePath>",
                @"         <ModulePath>.*datacollector.*</ModulePath>",
                @"         <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>",
                @"         <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>",
                @"         <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>",
                @"         <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>",
                @"         <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>",
                @"         <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>",
                @"         <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>",
                @"         <ModulePath>.*qtagent32.*</ModulePath>",
                @"         <ModulePath>.*msvcr.*dll$</ModulePath>",
                @"         <ModulePath>.*msvcp.*dll$</ModulePath>",
                @"         <ModulePath>.*clr.dll$</ModulePath>",
                @"         <ModulePath>.*clr.ni.dll$</ModulePath>",
                @"         <ModulePath>.*clrjit.dll$</ModulePath>",
                @"         <ModulePath>.*clrjit.ni.dll$</ModulePath>",
                @"         <ModulePath>.*mscoree.dll$</ModulePath>",
                @"         <ModulePath>.*mscoreei.dll$</ModulePath>",
                @"         <ModulePath>.*mscoreei.ni.dll$</ModulePath>",
                @"         <ModulePath>.*mscorlib.dll$</ModulePath>",
                @"         <ModulePath>.*mscorlib.ni.dll$</ModulePath>",
                @"         <ModulePath>.*cryptbase.dll$</ModulePath>",
                @"         <ModulePath>.*bcryptPrimitives.dll$</ModulePath>",
                @"       </Exclude>",
                @"    </ModulePaths>",
                @"    <Functions>",
                @"      <Exclude>",
                @"        <Function>^std::.*</Function>",
                @"        <Function>^ATL::.*</Function>",
                @"        <Function>.*::__GetTestMethodInfo.*</Function>",
                @"        <Function>.*__CxxPureMSILEntry.*</Function>",
                @"        <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>",
                @"        <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>",
                @"        <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>",
                @"        <Function>^__.*</Function>",
                @"        <Function>.*::__.*</Function>",
                @"      </Exclude>",
                @"    </Functions>",
                @"    <Attributes>",
                @"      <Exclude>",
                @"        <Attribute>^System.Diagnostics.DebuggerHiddenAttribute$</Attribute>",
                @"        <Attribute>^System.Diagnostics.DebuggerNonUserCodeAttribute$</Attribute>",
                @"        <Attribute>System.Runtime.CompilerServices.CompilerGeneratedAttribute$</Attribute>",
                @"        <Attribute>^System.CodeDom.Compiler.GeneratedCodeAttribute$</Attribute>",
                @"        <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute$</Attribute>",
                @"        <Attribute>^Microsoft.VisualStudio.TestPlatform.TestSDKAutoGeneratedCode.*</Attribute>",
                @"      </Exclude>",
                @"    </Attributes>",
                @"    <Sources>",
                @"      <Exclude>",
                @"          <Source>.*\\atlmfc\\.*</Source>",
                @"          <Source>.*\\atlmbfc\\.*</Source>",
                @"          <Source>.*\\vctools\\.*</Source>",
                @"          <Source>.*\\public\\sdk2\\.*</Source>",
                @"          <Source>.*\\externalapis\\.*</Source>",
                @"          <Source>.*\\microsoft sdks\\.*</Source>",
                @"          <Source>.*\\vc\\include\\.*</Source>",
                @"          <Source>.*\\msclr\\.*</Source>",
                @"          <Source>.*\\ucrt\\.*</Source>",
                @"          <Source>.*\\public\\sdk\\.*</Source>",
                @"      </Exclude>",
                @"    </Sources>",
                @"  </CodeCoverage>",
                @"</Configuration>");

            var expectedResultDocument = new XmlDocument();
            expectedResultDocument.LoadXml(expectedResult);

            var processedNode = processor.Process(settings);
            Assert.IsNotNull(processedNode);

            var codeCoverageNodes = this.ExtractNodes(processedNode, expectedResultDocument.DocumentElement, "./CodeCoverage");

            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./ModulePaths/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Functions/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Attributes/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Sources/Exclude");
        }
        #endregion

        #region Helpers
        private XmlNode ExtractNode(XmlNode node, string path)
        {
            try
            {
                return node.SelectSingleNode(path);
            }
            catch
            {
            }

            return null;
        }

        private XmlElement GetDefaultConfiguration()
        {
            var document = new XmlDocument();
            Assembly assembly = typeof(CodeCoverageRunSettingsProcessorTests).GetTypeInfo().Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(
                "Microsoft.TestPlatform.Utilities.UnitTests.DefaultCodeCoverageConfig.xml"))
            {
                document.Load(stream);
            }

            return document.DocumentElement;
        }

        private Tuple<XmlNode, XmlNode> ExtractNodes(XmlNode currentSettingsRoot, XmlNode defaultSettingsRoot, string path)
        {
            var currentNode = this.ExtractNode(currentSettingsRoot, path);
            var defaultNode = this.ExtractNode(defaultSettingsRoot, path);
            Assert.IsNotNull(currentNode);
            Assert.IsNotNull(defaultNode);

            return new Tuple<XmlNode, XmlNode>(currentNode, defaultNode);
        }

        private void CompareResults(XmlNode currentSettingsRoot, XmlNode defaultSettingsRoot, string path)
        {
            var nodes = this.ExtractNodes(currentSettingsRoot, defaultSettingsRoot, path);

            Assert.AreEqual(nodes.Item1.ChildNodes.Count, nodes.Item2.ChildNodes.Count);

            var set = new HashSet<string>();
            foreach (XmlNode child in nodes.Item1.ChildNodes)
            {
                if (!set.Contains(child.OuterXml))
                {
                    set.Add(child.OuterXml);
                }
            }

            foreach (XmlNode child in nodes.Item2.ChildNodes)
            {
                if (!set.Contains(child.OuterXml))
                {
                    set.Add(child.OuterXml);
                    continue;
                }

                set.Remove(child.OuterXml);
            }

            Assert.AreEqual(0, set.Count);
        }
        #endregion
    }
}
