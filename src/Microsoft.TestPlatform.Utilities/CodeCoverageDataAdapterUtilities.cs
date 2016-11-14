// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Globalization;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// The code coverage data adapter utilities.
    /// </summary>
    public static class CodeCoverageDataAdapterUtilities
    {
        #region private variables

        private const string DynamicCodeCoverageDataDiagnosticAdaterUriString = "datacollector://microsoft/CodeCoverage/2.0";
        private const string StaticCodeCoverageDataDiagnosticAdaterUriString = "datacollector://microsoft/CodeCoverage/1.0";

        private static string xPathSeperator = "/";
        private static string[] nodeNames = new string[] { Constants.RunSettingsName, Constants.DataCollectionRunSettingsName, Constants.DataCollectorsSettingName, Constants.DataCollectorSettingName };

        #region Default  CodeCoverage Settings String

        private static string codeCoverageCollectorSettingsTemplate =
@"      <DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=15.0.0.0 " + @", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" friendlyName=""Code Coverage"">" + Environment.NewLine +
@"        <Configuration>" + Environment.NewLine +
@"          <CodeCoverage>" + Environment.NewLine +
@"            <ModulePaths>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                 <ModulePath>.*CPPUnitTestFramework.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*vstest.console.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.intellitrace.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*vstest.executionengine.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*vstest.discoveryengine.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*qtagent32.*</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*msvcr.*dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*msvcp.*dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clr.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clr.ni.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clrjit.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*clrjit.ni.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscoree.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscoreei.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscoreei.ni.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscorlib.dll$</ModulePath>" + Environment.NewLine +
@"                 <ModulePath>.*mscorlib.ni.dll$</ModulePath>" + Environment.NewLine +
@"               </Exclude>" + Environment.NewLine +
@"            </ModulePaths>" + Environment.NewLine +
@"            <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>" + Environment.NewLine +
@"            <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>" + Environment.NewLine +
@"            <CollectFromChildProcesses>True</CollectFromChildProcesses>" + Environment.NewLine +
@"            <CollectAspDotNet>false</CollectAspDotNet>" + Environment.NewLine +
@"            <SymbolSearchPaths />" + Environment.NewLine +
@"            <Functions>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                <Function>^std::.*</Function>" + Environment.NewLine +
@"                <Function>^ATL::.*</Function>" + Environment.NewLine +
@"                <Function>.*::__GetTestMethodInfo.*</Function>" + Environment.NewLine +
@"                <Function>.*__CxxPureMSILEntry.*</Function>" + Environment.NewLine +
@"                <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>" + Environment.NewLine +
@"                <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>" + Environment.NewLine +
@"                <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>" + Environment.NewLine +
@"                <Function>^__.*</Function>" + Environment.NewLine +
@"                <Function>.*::__.*</Function>" + Environment.NewLine +
@"              </Exclude>" + Environment.NewLine +
@"            </Functions>" + Environment.NewLine +
@"            <Attributes>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                <Attribute>^System.Diagnostics.DebuggerHiddenAttribute$</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.Diagnostics.DebuggerNonUserCodeAttribute$</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.Runtime.CompilerServices.CompilerGeneratedAttribute$</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.CodeDom.Compiler.GeneratedCodeAttribute$</Attribute>" + Environment.NewLine +
@"                <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute$</Attribute>" + Environment.NewLine +
@"              </Exclude>" + Environment.NewLine +
@"            </Attributes>" + Environment.NewLine +
@"            <Sources>" + Environment.NewLine +
@"              <Exclude>" + Environment.NewLine +
@"                <Source>.*\\atlmfc\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\vctools\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\public\\sdk\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\externalapis\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\microsoft sdks\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\vc\\include\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\msclr\\.*</Source>" + Environment.NewLine +
@"                <Source>.*\\ucrt\\.*</Source>" + Environment.NewLine +
@"              </Exclude>" + Environment.NewLine +
@"            </Sources>" + Environment.NewLine +
@"            <CompanyNames/>" + Environment.NewLine +
@"            <PublicKeyTokens/>" + Environment.NewLine +
@"          </CodeCoverage>" + Environment.NewLine +
@"        </Configuration>" + Environment.NewLine +
@"      </DataCollector>";

        #endregion

        #endregion

        /// <summary>
        /// Updates with code coverage settings if not configured.
        /// </summary>
        /// <param name="runSettingsDocument"> The run settings document. </param>
        public static void UpdateWithCodeCoverageSettingsIfNotConfigured(IXPathNavigable runSettingsDocument)
        {
            ValidateArg.NotNull<IXPathNavigable>(runSettingsDocument, "runSettingsDocument");
            var runSettingsNavigator = runSettingsDocument.CreateNavigator();
            
            bool codeCoverageConfigured = XmlRunSettingsUtilities.ContainsDataCollector(runSettingsNavigator, DynamicCodeCoverageDataDiagnosticAdaterUriString)
                || XmlRunSettingsUtilities.ContainsDataCollector(runSettingsNavigator, StaticCodeCoverageDataDiagnosticAdaterUriString);

            if (codeCoverageConfigured == false)
            {
                var existingPath = string.Empty;
                var xpaths = new string[]
                             {
                                 string.Join(xPathSeperator, nodeNames, 0, 1),
                                 string.Join(xPathSeperator, nodeNames, 0, 2),
                                 string.Join(xPathSeperator, nodeNames, 0, 3)
                             };

                foreach (var xpath in xpaths)
                {
                    if (runSettingsNavigator.SelectSingleNode(xpath) != null)
                    {
                        existingPath = xpath;
                    }
                    else
                    {
                        break;
                    }
                }

                // If any nodes are missing to add code coverage deafult settings, add the missing xml nodes.
                XPathNavigator dataCollectorsNavigator;
                if (existingPath.Equals(xpaths[2]) == false)
                {
                    dataCollectorsNavigator = runSettingsNavigator.SelectSingleNode(existingPath);
                    var missingNodesText = GetMissingNodesTextIfAny(existingPath, xpaths[2]);
                    dataCollectorsNavigator.AppendChild(missingNodesText);
                }

                dataCollectorsNavigator = runSettingsNavigator.SelectSingleNode(xpaths[2]);
                dataCollectorsNavigator.AppendChild(codeCoverageCollectorSettingsTemplate);
            }
        }

        private static string GetMissingNodesTextIfAny(string existingPath, string fullpath)
        {
            var xmlText = "{0}";
            var nonExistingPath = fullpath.Substring(existingPath.Length);
            var requiredNodeNames = nonExistingPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var format = "<{0}>{1}</{0}>";

            foreach (var nodeName in requiredNodeNames)
            {
                xmlText = string.Format(CultureInfo.InvariantCulture, xmlText, string.Format(CultureInfo.InvariantCulture, format, nodeName, "{0}"));
            }

            xmlText = string.Format(CultureInfo.InvariantCulture, xmlText, string.Empty);
            return xmlText;
        }
    }
}
