// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    internal class ExclusionType
    {
        private string path;

        private IDictionary<string, XmlNode> exclusionRules;
  
        public ExclusionType(string path)
        {
            this.path = path;

            this.exclusionRules = new Dictionary<string, XmlNode>();
        }

        public void AddExclusionNodes(XmlDocument doc)
        {
            var masterNode = doc.SelectSingleNode(this.path);

            foreach (XmlNode child in masterNode.ChildNodes)
            {
                var key = child.OuterXml;
                if (this.exclusionRules.ContainsKey(key))
                {
                    continue;
                }

                this.exclusionRules.Add(key, child);
            }
        }

        public void ReplaceExclusionNodes(XmlDocument doc)
        {
            var masterNode = doc.SelectSingleNode(this.path);

            masterNode.RemoveAll();
            foreach (var child in this.exclusionRules.Values)
            {
                masterNode.AppendChild(child);
            }
        }
    }

    internal class CodeCoverageRunSettingsProcessor
    {
        private static readonly string CodeCoverageCollectorDefaultSettings =
            @"<DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=16.0.0.0 " + @", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" friendlyName=""Code Coverage"">" + Environment.NewLine +
            @"  <Configuration>" + Environment.NewLine +
            @"    <CodeCoverage>" + Environment.NewLine +
            @"      <ModulePaths>" + Environment.NewLine +
            @"        <Exclude>" + Environment.NewLine +
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

        private IEnumerable<ExclusionType> exclusions;

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

            this.exclusions = new List<ExclusionType>()
            {
                new ExclusionType(@"/DataCollector/Configuration/CodeCoverage/ModulePaths/Exclude"),
                new ExclusionType(@"/DataCollector/Configuration/CodeCoverage/Functions/Exclude"),
                new ExclusionType(@"/DataCollector/Configuration/CodeCoverage/Attributes/Exclude"),
                new ExclusionType(@"/DataCollector/Configuration/CodeCoverage/Sources/Exclude")
            };
        }

        public string Process()
        {
            this.Merge();
            this.Replace();

            return this.runSettingsDocument.OuterXml;
        }

        private void Merge()
        {
            foreach (var exclusionType in this.exclusions)
            {
                exclusionType.AddExclusionNodes(this.defaultRunSettingsDocument);
                exclusionType.AddExclusionNodes(this.runSettingsDocument);
            }
        }

        private void Replace()
        {
            foreach (var exclusionType in this.exclusions)
            {
                exclusionType.ReplaceExclusionNodes(this.runSettingsDocument);
            }
        }
    }
}
