// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// 
    /// </summary>
    public class CodeCoverageRunSettingsProcessor
    {
        #region Type Members
        /// <summary>
        /// 
        /// </summary>
        private class Exclusion
        {
            /// <summary>
            /// 
            /// </summary>
            private string path;

            /// <summary>
            /// 
            /// </summary>
            public IEnumerable<string> PathComponents { get; }

            /// <summary>
            /// 
            /// </summary>
            public IDictionary<string, XmlNode> ExclusionRules { get; }

            /// <summary>
            /// 
            /// </summary>
            public string Path
            {
                get
                {
                    if (string.IsNullOrEmpty(this.path))
                    {
                        this.path = Exclusion.BuildPath(this.PathComponents);
                    }

                    return this.path;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// 
            /// <param name="pathComponents"></param>
            public Exclusion(IEnumerable<string> pathComponents)
            {
                this.path = string.Empty;
                this.PathComponents = pathComponents;
                this.ExclusionRules = new Dictionary<string, XmlNode>();
            }

            /// <summary>
            /// 
            /// </summary>
            /// 
            /// <returns></returns>
            public static string BuildPath(IEnumerable<string> pathComponents)
            {
                var path = ".";

                foreach (var component in pathComponents)
                {
                    path += "/" + component;
                }

                return path;
            }
        }
        #endregion

        #region Members
        #region Default Settings String
        /// <summary>
        /// 
        /// </summary>
        private static readonly string DefaultSettings =
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
        #endregion

        /// <summary>
        /// 
        /// </summary>
        private XmlDocument defaultSettingsDocument;

        /// <summary>
        /// 
        /// </summary>
        private XmlDocument currentSettingsDocument;

        /// <summary>
        /// 
        /// </summary>
        private IEnumerable<Exclusion> exclusions;
        #endregion

        #region Constructors & Helpers
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="currentSettings"></param>
        public CodeCoverageRunSettingsProcessor(string currentSettings)
        {
            ValidateArg.NotNullOrEmpty(currentSettings, nameof(currentSettings));

            this.currentSettingsDocument = new XmlDocument();
            this.currentSettingsDocument.LoadXml(currentSettings);

            this.Initialize(currentSettingsDocument);
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="currentSettingsDocument"></param>
        public CodeCoverageRunSettingsProcessor(XmlDocument currentSettingsDocument)
        {
            ValidateArg.NotNull(currentSettingsDocument, nameof(currentSettingsDocument));

            this.Initialize(currentSettingsDocument);
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="currentSettingsDocument"></param>
        private void Initialize(XmlDocument currentSettingsDocument)
        {
            this.currentSettingsDocument = currentSettingsDocument;

            this.defaultSettingsDocument = new XmlDocument();
            this.defaultSettingsDocument.LoadXml(CodeCoverageRunSettingsProcessor.DefaultSettings);

            this.exclusions = new List<Exclusion>
            {
                new Exclusion(new List<string> { "ModulePaths", "Exclude" }),
                new Exclusion(new List<string> { "Attributes", "Exclude" }),
                new Exclusion(new List<string> { "Sources", "Exclude" }),
                new Exclusion(new List<string> { "Functions", "Exclude" })
            };
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <returns></returns>
        public string Process()
        {
            var dataCollectorNode = this.GetDataCollectorNode();
            if (dataCollectorNode == null)
            {
                // No data collector settings for code coverage are found.
                // TODO: Consider adding defaults nevertheless.
                return this.currentSettingsDocument.OuterXml;
            }

            var codeCoveragePathComponents = new List<string>() { "Configuration", "CodeCoverage" };
            var currentCodeCoverageNode = this.SelectNodeOrAddDefaults(
                dataCollectorNode,
                this.defaultSettingsDocument.FirstChild,
                codeCoveragePathComponents);

            if (currentCodeCoverageNode == null)
            {
                return this.currentSettingsDocument.OuterXml;
            }

            var defaultCodeCoverageNode = this.ExtractNode(
                this.defaultSettingsDocument.FirstChild,
                Exclusion.BuildPath(codeCoveragePathComponents));

            foreach (var exclusion in this.exclusions)
            {
                var currentNode = this.SelectNodeOrAddDefaults(
                    currentCodeCoverageNode,
                    defaultCodeCoverageNode,
                    exclusion.PathComponents);
                if (currentNode == null || !this.ShouldProcessCurrentExclusion(currentNode))
                {
                    continue;
                }

                var defaultNode = this.ExtractNode(
                    defaultCodeCoverageNode,
                    exclusion.Path);

                this.AddNodes(defaultNode, exclusion);
                this.AddNodes(currentNode, exclusion);

                this.ReplaceNodes(currentNode, exclusion);
            }

            return this.currentSettingsDocument.OuterXml;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <returns></returns>
        private XmlNode GetDataCollectorNode()
        {
            const string prefixPath = @"/RunSettings/DataCollectionRunSettings/DataCollectors";
            const string attributeName = "uri";
            const string attributeValue = "datacollector://microsoft/CodeCoverage/2.0";

            var dataCollectorsNode = this.ExtractNode(this.currentSettingsDocument, prefixPath);
            if (dataCollectorsNode == null)
            {
                return dataCollectorsNode;
            }

            foreach (XmlNode node in dataCollectorsNode.ChildNodes)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute.Name == attributeName && attribute.Value == attributeValue)
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="rootNode"></param>
        /// <param name="pathComponents"></param>
        /// 
        /// <returns></returns>
        private XmlNode SelectNodeOrAddDefaults(
            XmlNode currentRootNode,
            XmlNode defaultRootNode,
            IEnumerable<string> pathComponents)
        {
            var currentNode = currentRootNode;
            var partialPath = ".";

            foreach (var component in pathComponents)
            {
                var currentPathComponent = "/" + component;

                partialPath += currentPathComponent;
                var tempNode = this.ExtractNode(currentNode, "." + currentPathComponent);

                if (tempNode == null)
                {
                    var defaultNode = this.ExtractNode(
                        defaultRootNode,
                        partialPath);

                    var importedChild = currentNode.OwnerDocument.ImportNode(defaultNode, true);
                    currentNode.AppendChild(importedChild);

                    return null;
                }

                currentNode = tempNode;
            }

            return currentNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="node"></param>
        /// 
        /// <returns></returns>
        private bool ShouldProcessCurrentExclusion(XmlNode node)
        {
            const string attributeName = "mergeDefaults";
            const string attributeValue = "false";

            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.Name == attributeName && attribute.Value == attributeValue)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="node"></param>
        /// <param name="path"></param>
        /// 
        /// <returns></returns>
        private XmlNode ExtractNode(XmlNode node, string path)
        {
            try
            {
                return node.SelectSingleNode(path);
            }
            catch (XPathException ex)
            {
                EqtTrace.Error(
                    "CodeCoverageRunSettingsProcessor.ExtractNode: Cannot select single node \"{0}\".",
                    ex.Message);
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="node"></param>
        /// <param name="exclusion"></param>
        private void AddNodes(XmlNode node, Exclusion exclusion)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var key = child.OuterXml;
                if (exclusion.ExclusionRules.ContainsKey(key))
                {
                    continue;
                }

                exclusion.ExclusionRules.Add(key, child);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="node"></param>
        /// <param name="exclusion"></param>
        private void ReplaceNodes(XmlNode node, Exclusion exclusion)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (!exclusion.ExclusionRules.ContainsKey(child.OuterXml))
                {
                    continue;
                }

                exclusion.ExclusionRules.Remove(child.OuterXml);
            }

            foreach (var child in exclusion.ExclusionRules.Values)
            {
                var importedChild = node.OwnerDocument.ImportNode(child, true);
                node.AppendChild(importedChild);
            }

            exclusion.ExclusionRules.Clear();
        }
        #endregion
    }
}
