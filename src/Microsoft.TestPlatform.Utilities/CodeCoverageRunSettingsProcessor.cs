// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.XPath;

    public class CodeCoverageRunSettingsProcessor
    {
        #region Members
        private static readonly string CodeCoverageCollectorDefaultSettings =
            @"<DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=16.0.0.0 " + @", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" friendlyName=""Code Coverage"">" + Environment.NewLine +
            @"  <Configuration>" + Environment.NewLine +
            @"    <CodeCoverage>" + Environment.NewLine +
            @"      <ModulePaths>" + Environment.NewLine +
            @"        <Exclude mergeDefaults='true'>" + Environment.NewLine +
            @"           <ModulePath>.*CPPUnitTestFramework.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*vstest.console.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.intellitrace.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*testhost.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*datacollector.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*qtagent32.*</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*msvcr.*dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*msvcp.*dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*clr.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*clr.ni.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*clrjit.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*clrjit.ni.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*mscoree.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*mscoreei.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*mscoreei.ni.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*mscorlib.dll$</ModulePath>" + Environment.NewLine +
            @"           <ModulePath>.*mscorlib.ni.dll$</ModulePath>" + Environment.NewLine +
            @"         </Exclude>" + Environment.NewLine +
            @"      </ModulePaths>" + Environment.NewLine +
            @"      <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>" + Environment.NewLine +
            @"      <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>" + Environment.NewLine +
            @"      <CollectFromChildProcesses>True</CollectFromChildProcesses>" + Environment.NewLine +
            @"      <CollectAspDotNet>false</CollectAspDotNet>" + Environment.NewLine +
            @"      <SymbolSearchPaths />" + Environment.NewLine +
            @"      <Functions>" + Environment.NewLine +
            @"        <Exclude>" + Environment.NewLine +
            @"          <Function>^std::.*</Function>" + Environment.NewLine +
            @"          <Function>^ATL::.*</Function>" + Environment.NewLine +
            @"          <Function>.*::__GetTestMethodInfo.*</Function>" + Environment.NewLine +
            @"          <Function>.*__CxxPureMSILEntry.*</Function>" + Environment.NewLine +
            @"          <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>" + Environment.NewLine +
            @"          <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>" + Environment.NewLine +
            @"          <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>" + Environment.NewLine +
            @"          <Function>^__.*</Function>" + Environment.NewLine +
            @"          <Function>.*::__.*</Function>" + Environment.NewLine +
            @"        </Exclude>" + Environment.NewLine +
            @"      </Functions>" + Environment.NewLine +
            @"      <Attributes>" + Environment.NewLine +
            @"        <Exclude>" + Environment.NewLine +
            @"          <Attribute>^System.Diagnostics.DebuggerHiddenAttribute$</Attribute>" + Environment.NewLine +
            @"          <Attribute>^System.Diagnostics.DebuggerNonUserCodeAttribute$</Attribute>" + Environment.NewLine +
            @"          <Attribute>^System.Runtime.CompilerServices.CompilerGeneratedAttribute$</Attribute>" + Environment.NewLine +
            @"          <Attribute>^System.CodeDom.Compiler.GeneratedCodeAttribute$</Attribute>" + Environment.NewLine +
            @"          <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute$</Attribute>" + Environment.NewLine +
            @"          <Attribute>^Microsoft.VisualStudio.TestPlatform.TestSDKAutoGeneratedCode.*</Attribute>" + Environment.NewLine +
            @"        </Exclude>" + Environment.NewLine +
            @"      </Attributes>" + Environment.NewLine +
            @"      <Sources>" + Environment.NewLine +
            @"        <Exclude>" + Environment.NewLine +
            @"          <Source>.*\\atlmfc\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\vctools\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\public\\sdk\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\externalapis\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\microsoft sdks\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\vc\\include\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\msclr\\.*</Source>" + Environment.NewLine +
            @"          <Source>.*\\ucrt\\.*</Source>" + Environment.NewLine +
            @"        </Exclude>" + Environment.NewLine +
            @"      </Sources>" + Environment.NewLine +
            @"      <CompanyNames/>" + Environment.NewLine +
            @"      <PublicKeyTokens/>" + Environment.NewLine +
            @"    </CodeCoverage>" + Environment.NewLine +
            @"  </Configuration>" + Environment.NewLine +
            @"</DataCollector>";

        private XmlDocument defaultRunSettingsDocument;

        private XmlDocument runSettingsDocument;

        private IEnumerable<Tuple<string, IDictionary<string, XmlNode>>> exclusionClasses;
        #endregion

        #region Constructors & Helpers
        public CodeCoverageRunSettingsProcessor(string runSettings)
        {
            ValidateArg.NotNullOrEmpty(runSettings, nameof(runSettings));

            runSettingsDocument = new XmlDocument();
            runSettingsDocument.LoadXml(runSettings);

            this.Initialize(runSettingsDocument);
        }

        public CodeCoverageRunSettingsProcessor(XmlDocument runSettingsDocument)
        {
            ValidateArg.NotNull(runSettingsDocument, nameof(runSettingsDocument));

            this.Initialize(runSettingsDocument);
        }

        private void Initialize(XmlDocument runSettingsDocument)
        {
            this.runSettingsDocument = runSettingsDocument;

            defaultRunSettingsDocument = new XmlDocument();
            defaultRunSettingsDocument.LoadXml(CodeCoverageRunSettingsProcessor.CodeCoverageCollectorDefaultSettings);

            this.exclusionClasses = new List<Tuple<string, IDictionary<string, XmlNode>>>()
            {
                new Tuple<string, IDictionary<string, XmlNode>>(
                    @"./Configuration/CodeCoverage/ModulePaths/Exclude",
                    new Dictionary<string, XmlNode>()),

                new Tuple<string, IDictionary<string, XmlNode>>(
                    @"./Configuration/CodeCoverage/Attributes/Exclude",
                    new Dictionary<string, XmlNode>()),

                new Tuple<string, IDictionary<string, XmlNode>>(
                    @"./Configuration/CodeCoverage/Sources/Exclude",
                    new Dictionary<string, XmlNode>()),

                new Tuple<string, IDictionary<string, XmlNode>>(
                    @"./Configuration/CodeCoverage/Functions/Exclude",
                    new Dictionary<string, XmlNode>())
            };
        }
        #endregion

        #region Public Interface
        public string Process()
        {
            var codeCoverageDataCollectorNode = GetCodeCoverageDataCollectorNode();
            if (codeCoverageDataCollectorNode == null)
            {
                // What do we do if we cannot extract code coverage data collectors node ?
                return this.runSettingsDocument.OuterXml;
            }

            foreach (var exclusionClass in this.exclusionClasses)
            {
                var node = this.ExtractNode(codeCoverageDataCollectorNode, exclusionClass.Item1);
                if (node == null)
                {
                    // What do we do if we cannot extract the specified class ?
                    continue;
                }

                if (!this.ShouldProcessCurrentExclusionClass(node))
                {
                    continue;
                }

                var defaultNode = this.ExtractNode(this.defaultRunSettingsDocument.FirstChild, exclusionClass.Item1);

                AddNodes(defaultNode, exclusionClass);
                AddNodes(node, exclusionClass);

                ReplaceNodes(node, exclusionClass);
            }

            return this.runSettingsDocument.OuterXml;
        }
        #endregion

        #region Private Methods
        private XmlNode GetCodeCoverageDataCollectorNode()
        {
            const string prefix = @"/RunSettings/DataCollectionRunSettings/DataCollectors";

            var dataCollectorsNode = this.ExtractNode(this.runSettingsDocument, prefix);
            if (dataCollectorsNode == null)
            {
                // Should we return default exclusions in this case ?
                return dataCollectorsNode;
            }

            foreach (XmlNode node in dataCollectorsNode.ChildNodes)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute.Name == "uri" && attribute.Value == "datacollector://microsoft/CodeCoverage/2.0")
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        private bool ShouldProcessCurrentExclusionClass(XmlNode node)
        {
            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.Name == "mergeDefaults" && attribute.Value == "false")
                {
                    return false;
                }
            }

            return true;
        }

        private XmlNode ExtractNode(XmlNode doc, string path)
        {
            try
            {
                return doc.SelectSingleNode(path);
            }
            catch (XPathException ex)
            {
                EqtTrace.Error("CodeCoverageRunSettingsProcessor.ExtractNode: Cannot select single node \"{0}\".", ex.Message);
            }

            return null;
        }

        private void AddNodes(XmlNode node, Tuple<string, IDictionary<string, XmlNode>> exclusionClass)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var key = child.OuterXml;
                if (exclusionClass.Item2.ContainsKey(key))
                {
                    continue;
                }

                exclusionClass.Item2.Add(key, child);
            }
        }

        private void ReplaceNodes(XmlNode node, Tuple<string, IDictionary<string, XmlNode>> exclusionClass)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (!exclusionClass.Item2.ContainsKey(child.OuterXml))
                {
                    continue;
                }

                exclusionClass.Item2.Remove(child.OuterXml);
            }

            foreach (var child in exclusionClass.Item2.Values)
            {
                var imported = node.OwnerDocument.ImportNode(child, true);
                node.AppendChild(imported);
            }

            exclusionClass.Item2.Clear();
        }
        #endregion
    }
}
