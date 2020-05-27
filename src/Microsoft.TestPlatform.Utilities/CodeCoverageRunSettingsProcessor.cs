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
    /// Represents the run settings processor for code coverage data collectors.
    /// </summary>
    public class CodeCoverageRunSettingsProcessor
    {
        #region Type Members
        /// <summary>
        /// Represents the exclusion type for code coverage run settings.
        /// </summary>
        private class Exclusion
        {
            /// <summary>
            /// Represents the <see cref="XPathNavigator"/> style path of the exclusion type.
            /// </summary>
            private string path;

            /// <summary>
            /// Gets the path components for the current exclusion type.
            /// </summary>
            public IEnumerable<string> PathComponents { get; }

            /// <summary>
            /// Gets the exclusion rules for the current exclusion type.
            /// </summary>
            public IDictionary<string, XmlNode> ExclusionRules { get; }

            /// <summary>
            /// Gets the actual exclusion type path generated from the individual path components.
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
            /// Constructs an <see cref="Exclusion"/> object.
            /// </summary>
            /// 
            /// <param name="pathComponents">The path split in components.</param>
            public Exclusion(IEnumerable<string> pathComponents)
            {
                this.path = string.Empty;
                this.PathComponents = pathComponents;
                this.ExclusionRules = new Dictionary<string, XmlNode>();
            }

            /// <summary>
            /// Assembles a relative path from the path given as components.
            /// </summary>
            /// 
            /// <returns>A relative path built from path components.</returns>
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
        /// Represents the default settings for the code coverage data collector.
        /// </summary>
        private static readonly string DefaultSettings = string.Join(
            Environment.NewLine,
            @"<DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=16.0.0.0 " + @", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" friendlyName=""Code Coverage"">",
            @"  <Configuration>",
            @"    <CodeCoverage>",
            @"      <ModulePaths>",
            @"        <Exclude>",
            @"           <ModulePath>.*CPPUnitTestFramework.*</ModulePath>",
            @"           <ModulePath>.*vstest.console.*</ModulePath>",
            @"           <ModulePath>.*microsoft.intellitrace.*</ModulePath>",
            @"           <ModulePath>.*testhost.*</ModulePath>",
            @"           <ModulePath>.*datacollector.*</ModulePath>",
            @"           <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>",
            @"           <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>",
            @"           <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>",
            @"           <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>",
            @"           <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>",
            @"           <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>",
            @"           <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>",
            @"           <ModulePath>.*qtagent32.*</ModulePath>",
            @"           <ModulePath>.*msvcr.*dll$</ModulePath>",
            @"           <ModulePath>.*msvcp.*dll$</ModulePath>",
            @"           <ModulePath>.*clr.dll$</ModulePath>",
            @"           <ModulePath>.*clr.ni.dll$</ModulePath>",
            @"           <ModulePath>.*clrjit.dll$</ModulePath>",
            @"           <ModulePath>.*clrjit.ni.dll$</ModulePath>",
            @"           <ModulePath>.*mscoree.dll$</ModulePath>",
            @"           <ModulePath>.*mscoreei.dll$</ModulePath>",
            @"           <ModulePath>.*mscoreei.ni.dll$</ModulePath>",
            @"           <ModulePath>.*mscorlib.dll$</ModulePath>",
            @"           <ModulePath>.*mscorlib.ni.dll$</ModulePath>",
            @"           <ModulePath>.*cryptbase.dll$</ModulePath>",
            @"           <ModulePath>.*bcryptPrimitives.dll$</ModulePath>",
            @"         </Exclude>",
            @"      </ModulePaths>",
            @"      <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>",
            @"      <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>",
            @"      <CollectFromChildProcesses>True</CollectFromChildProcesses>",
            @"      <CollectAspDotNet>false</CollectAspDotNet>",
            @"      <SymbolSearchPaths />",
            @"      <Functions>",
            @"        <Exclude>",
            @"          <Function>^std::.*</Function>",
            @"          <Function>^ATL::.*</Function>",
            @"          <Function>.*::__GetTestMethodInfo.*</Function>",
            @"          <Function>.*__CxxPureMSILEntry.*</Function>",
            @"          <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>",
            @"          <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>",
            @"          <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>",
            @"          <Function>^__.*</Function>",
            @"          <Function>.*::__.*</Function>",
            @"        </Exclude>",
            @"      </Functions>",
            @"      <Attributes>",
            @"        <Exclude>",
            @"          <Attribute>^System.Diagnostics.DebuggerHiddenAttribute$</Attribute>",
            @"          <Attribute>^System.Diagnostics.DebuggerNonUserCodeAttribute$</Attribute>",
            @"          <Attribute>System.Runtime.CompilerServices.CompilerGeneratedAttribute$</Attribute>",
            @"          <Attribute>^System.CodeDom.Compiler.GeneratedCodeAttribute$</Attribute>",
            @"          <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute$</Attribute>",
            @"          <Attribute>^Microsoft.VisualStudio.TestPlatform.TestSDKAutoGeneratedCode.*</Attribute>",
            @"        </Exclude>",
            @"      </Attributes>",
            @"      <Sources>",
            @"        <Exclude>",
            @"          <Source>.*\\atlmfc\\.*</Source>",
            @"          <Source>.*\\vctools\\.*</Source>",
            @"          <Source>.*\\public\\sdk\\.*</Source>",
            @"          <Source>.*\\externalapis\\.*</Source>",
            @"          <Source>.*\\microsoft sdks\\.*</Source>",
            @"          <Source>.*\\vc\\include\\.*</Source>",
            @"          <Source>.*\\msclr\\.*</Source>",
            @"          <Source>.*\\ucrt\\.*</Source>",
            @"        </Exclude>",
            @"      </Sources>",
            @"      <CompanyNames/>",
            @"      <PublicKeyTokens/>",
            @"    </CodeCoverage>",
            @"  </Configuration>",
            @"</DataCollector>");
        #endregion

        /// <summary>
        /// Represents the default settings loaded as an <see cref="XmlNode"/>.
        /// </summary>
        private XmlNode defaultSettingsRootNode;

        /// <summary>
        /// Represents a list of exclusion types tracked by this processor.
        /// </summary>
        private IEnumerable<Exclusion> exclusions;
        #endregion

        #region Constructors & Helpers
        /// <summary>
        /// Constructs an <see cref="CodeCoverageRunSettingsProcessor"/> object.
        /// </summary>
        public CodeCoverageRunSettingsProcessor()
        {
            // Load default settings from string.
            var document = new XmlDocument();
            document.LoadXml(CodeCoverageRunSettingsProcessor.DefaultSettings);

            this.defaultSettingsRootNode = document.DocumentElement.FirstChild;

            // Create the exclusion type list.
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
        /// Processes the current settings for the code coverage data collector.
        /// </summary>
        /// 
        /// <param name="currentSettings">The code coverage settings.</param>
        /// 
        /// <returns>An updated version of the current run settings.</returns>
        public XmlNode Process(string currentSettings)
        {
            if (string.IsNullOrEmpty(currentSettings))
            {
                return null;
            }

            // Load current settings from string.
            var document = new XmlDocument();
            document.LoadXml(currentSettings);

            return this.Process(document.DocumentElement);
        }

        /// <summary>
        /// Processes the current settings for the code coverage data collector.
        /// </summary>
        /// 
        /// <param name="currentSettingsDocument">
        /// The code coverage settings document.
        /// </param>
        /// 
        /// <returns>An updated version of the current run settings.</returns>
        public XmlNode Process(XmlDocument currentSettingsDocument)
        {
            if (currentSettingsDocument == null)
            {
                return null;
            }

            return this.Process(currentSettingsDocument.DocumentElement);
        }

        /// <summary>
        /// Processes the current settings for the code coverage data collector.
        /// </summary>
        /// 
        /// <param name="currentSettingsRootNode">The code coverage root element.</param>
        /// 
        /// <returns>An updated version of the current run settings.</returns>
        public XmlNode Process(XmlNode currentSettingsRootNode)
        {
            if (currentSettingsRootNode == null)
            {
                return null;
            }

            // Get the code coverage node from the current settings. If unable to get any
            // particular component down the path just add the default values for that component
            // from the default settings document and return since there's nothing else to be done.
            var codeCoveragePathComponents = new List<string>() { "CodeCoverage" };
            var currentCodeCoverageNode = this.SelectNodeOrAddDefaults(
                currentSettingsRootNode,
                this.defaultSettingsRootNode,
                codeCoveragePathComponents);

            if (currentCodeCoverageNode == null)
            {
                return currentSettingsRootNode;
            }

            // Get the code coverage node from the default settings.
            var defaultCodeCoverageNode = this.ExtractNode(
                this.defaultSettingsRootNode,
                Exclusion.BuildPath(codeCoveragePathComponents));

            foreach (var exclusion in this.exclusions)
            {
                // Get the <Exclude> node for the current exclusion type. If unable to get any
                // particular component down the path just add the default values for that
                // component from the default settings document and continue since there's nothing
                // else to be done.
                var currentNode = this.SelectNodeOrAddDefaults(
                    currentCodeCoverageNode,
                    defaultCodeCoverageNode,
                    exclusion.PathComponents);

                // Check if the node extraction was successful and we should process the current
                // node in order to merge the current exclusion rules with the default ones.
                if (currentNode == null || !this.ShouldProcessCurrentExclusion(currentNode))
                {
                    continue;
                }

                // Extract the <Exclude> node from the default settings.
                var defaultNode = this.ExtractNode(
                    defaultCodeCoverageNode,
                    exclusion.Path);

                // Add nodes from both the current and the default settings to current exclusion
                // type's exclusion rules.
                this.AddNodes(defaultNode, exclusion);
                this.AddNodes(currentNode, exclusion);

                // Merge both the current and the default settings for the current exclusion rule.
                this.MergeNodes(currentNode, exclusion);
            }

            return currentSettingsRootNode;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Selects the node from the current settings node using the given
        /// <see cref="XPathNavigator"/> style path. If unable to select the requested node it adds
        /// default settings along the path.
        /// </summary>
        /// 
        /// <param name="currentRootNode">
        /// The root node from the current settings document for the extraction.
        /// </param>
        /// <param name="defaultRootNode">
        /// The corresponding root node from the default settings document.
        /// </param>
        /// <param name="pathComponents">The path components.</param>
        /// 
        /// <returns>The requested node if successful, <see cref="null"/> otherwise.</returns>
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

                // Append the current path component to the partial path.
                partialPath += currentPathComponent;

                // Extract the node corresponding to the latest path component.
                var tempNode = this.ExtractNode(currentNode, "." + currentPathComponent);

                // If the current node extraction is unsuccessful then add the corresponding
                // default settings node and bail out.
                if (tempNode == null)
                {
                    var defaultNode = this.ExtractNode(
                        defaultRootNode,
                        partialPath);

                    var importedChild = currentNode.OwnerDocument.ImportNode(defaultNode, true);
                    currentNode.AppendChild(importedChild);

                    return null;
                }

                // Node corresponding to the latest path component is the new root node for the
                // next extraction.
                currentNode = tempNode;
            }

            return currentNode;
        }

        /// <summary>
        /// Checks if we should process the current exclusion node.
        /// </summary>
        /// 
        /// <param name="node">The current exclusion node.</param>
        /// 
        /// <returns>
        /// <see cref="true"/> if the node should be processed, <see cref="false"/> otherwise.
        /// </returns>
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
        /// Extracts the node specified by the current path using the provided node as root.
        /// </summary>
        /// 
        /// <param name="node">The root to be used for extraction.</param>
        /// <param name="path">The path used to specify the requested node.</param>
        /// 
        /// <returns>The extracted node if successful, <see cref="null"/> otherwise.</returns>
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
        /// Adds all children nodes of the current root to the current exclusion type's exclusion
        /// rules cache.
        /// </summary>
        /// 
        /// <param name="node">The root node.</param>
        /// <param name="exclusion">The exclusion rule.</param>
        private void AddNodes(XmlNode node, Exclusion exclusion)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var key = child.OuterXml;

                // Ignore keys that are already present in the current exclusion type's exclusion
                // rules cache.
                if (exclusion.ExclusionRules.ContainsKey(key))
                {
                    continue;
                }

                // Add the current exclusion rule to the exclusion type's cache.
                exclusion.ExclusionRules.Add(key, child);
            }
        }

        /// <summary>
        /// Merges the current settings rules with the default settings rules.
        /// </summary>
        /// 
        /// <param name="node">The root node.</param>
        /// <param name="exclusion">The exclusion rule.</param>
        private void MergeNodes(XmlNode node, Exclusion exclusion)
        {
            // Iterate through all the children nodes of the given root.
            foreach (XmlNode child in node.ChildNodes)
            {
                if (!exclusion.ExclusionRules.ContainsKey(child.OuterXml))
                {
                    continue;
                }

                // Remove exclusion rule from the current exclusion type's cache.
                exclusion.ExclusionRules.Remove(child.OuterXml);
            }

            // Iterate through remaining items in the current exclusion type cache.
            foreach (var child in exclusion.ExclusionRules.Values)
            {
                // Import any remaining items in the current settings document.
                var importedChild = node.OwnerDocument.ImportNode(child, true);
                node.AppendChild(importedChild);
            }

            exclusion.ExclusionRules.Clear();
        }
        #endregion
    }
}
