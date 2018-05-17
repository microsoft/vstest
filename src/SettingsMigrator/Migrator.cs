// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Globalization;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Migrator used to migrate test settings and run settings with embedded testsettings to run settings.
    /// </summary>
    public class Migrator
    {
        /// <summary>
        /// Given a runSettings with an embedded testSettings, converts it to runSettings.
        /// </summary>
        /// <param name="oldRunSettingsPath"></param>
        /// <param name="newRunSettingsPath"></param>
        public void MigrateRunSettings(string oldRunSettingsPath, string newRunSettingsPath)
        {
            string testSettingsPath = null;
            using (XmlTextReader reader = new XmlTextReader(oldRunSettingsPath))
            {
                reader.Namespaces = false;

                var runSettingsXmlDoc = new XmlDocument();
                runSettingsXmlDoc.Load(reader);
                var root = runSettingsXmlDoc.DocumentElement;

                var testSettingsNode = root.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");

                if (testSettingsNode != null)
                {
                    testSettingsPath = testSettingsNode.InnerText;
                }
                if (!string.IsNullOrWhiteSpace(testSettingsPath))
                {
                    // Expand path relative to runSettings location.
                    if (!Path.IsPathRooted(testSettingsPath))
                    {
                        testSettingsPath = Path.Combine(oldRunSettingsPath, testSettingsPath);
                    }

                    MigrateTestSettingsNodesToRunSettings(testSettingsPath, runSettingsXmlDoc);

                    runSettingsXmlDoc.Save(newRunSettingsPath);
                }
                else
                {
                    Console.WriteLine("RunSettings does not contain an embedded testSettings, not migrating.");
                }
            }
        }

        /// <summary>
        /// Given a testSettings, converts it to runSettings.
        /// </summary>
        /// <param name="oldTestSettingsPath"></param>
        /// <param name="newRunSettingsPath"></param>
        public void MigrateTestSettings(string oldTestSettingsPath, string newRunSettingsPath)
        {
            var runSettingsXmlDoc = new XmlDocument();
            runSettingsXmlDoc.LoadXml(sampleRunSettingsContent);

            MigrateTestSettingsNodesToRunSettings(oldTestSettingsPath, runSettingsXmlDoc);

            runSettingsXmlDoc.Save(newRunSettingsPath);
        }

        /// <summary>
        /// Given a testSettings, converts it to runSettings
        /// </summary>
        /// <param name="testSettingsPath"></param>
        /// <param name="newRunSettingsPath"></param>
        /// <param name="oldRunSettingsContent"></param>
        private void MigrateTestSettingsNodesToRunSettings(string testSettingsPath, XmlDocument runSettingsXmlDoc)
        {
            var testSettingsNodes = ReadTestSettingsNodes(testSettingsPath);

            string testTimeout = null;
            if (testSettingsNodes.Timeout != null && testSettingsNodes.Timeout.Attributes[TestTimeoutAttributeName] != null)
            {
                testTimeout = testSettingsNodes.Timeout.Attributes[TestTimeoutAttributeName].Value;
            }

            string runTimeout = null;
            if (testSettingsNodes.Timeout != null && testSettingsNodes.Timeout.Attributes[RunTimeoutAttributeName] != null)
            {
                runTimeout = testSettingsNodes.Timeout.Attributes[RunTimeoutAttributeName].Value;
            }

            string parallelTestCount = null;
            if (testSettingsNodes.Execution != null && testSettingsNodes.Execution.Attributes[ParallelTestCountAttributeName] != null)
            {
                parallelTestCount = testSettingsNodes.Execution.Attributes[ParallelTestCountAttributeName].Value;
            }

            // Remove the embedded testSettings node if it exists.
            RemoveEmbeddedTestSettings(runSettingsXmlDoc);

            // WebTestRunConfiguration node.
            if (testSettingsNodes.WebSettings != null)
            {
                runSettingsXmlDoc.DocumentElement.AppendChild(runSettingsXmlDoc.ImportNode(testSettingsNodes.WebSettings, deep: true));
            }

            // LegacySettings node.
            if (testSettingsNodes.Deployment != null || testSettingsNodes.Script != null || testSettingsNodes.UnitTestConfig != null ||
                !string.IsNullOrEmpty(parallelTestCount) || !string.IsNullOrEmpty(testTimeout) || testSettingsNodes.Hosts != null)
            {
                AddLegacyNodes(testSettingsNodes, testTimeout, parallelTestCount, runSettingsXmlDoc);
            }

            // TestSessionTimeout node.
            if (!string.IsNullOrEmpty(runTimeout))
            {
                AddRunTimeoutNode(runTimeout, runSettingsXmlDoc);
            }

            // DataCollectors node.
            if (testSettingsNodes.Datacollectors != null && testSettingsNodes.Datacollectors.Count > 0)
            {
                AddDataCollectorNodes(testSettingsNodes.Datacollectors, runSettingsXmlDoc);
            }
        }

        private TestSettingsNodes ReadTestSettingsNodes(string testSettingsPath)
        {
            var testSettingsNodes = new TestSettingsNodes();

            using (XmlTextReader reader = new XmlTextReader(testSettingsPath))
            {
                reader.Namespaces = false;

                var testSettingsXmlDoc = new XmlDocument();
                testSettingsXmlDoc.Load(reader);
                var testSettingsRoot = testSettingsXmlDoc.DocumentElement;

                // Select the interesting nodes from the xml.
                testSettingsNodes.Deployment = testSettingsRoot.SelectSingleNode(@"/TestSettings/Deployment");
                testSettingsNodes.Script = testSettingsRoot.SelectSingleNode(@"/TestSettings/Scripts");
                testSettingsNodes.WebSettings = testSettingsRoot.SelectSingleNode(@"/TestSettings/Execution/TestTypeSpecific/WebTestRunConfiguration");
                testSettingsNodes.Datacollectors = testSettingsRoot.SelectNodes(@"/TestSettings/AgentRule/DataCollectors/DataCollector");
                testSettingsNodes.Timeout = testSettingsRoot.SelectSingleNode(@"/TestSettings/Execution/Timeouts");
                testSettingsNodes.UnitTestConfig = testSettingsRoot.SelectSingleNode(@"/TestSettings/Execution/TestTypeSpecific/UnitTestRunConfig");
                testSettingsNodes.Hosts = testSettingsRoot.SelectSingleNode(@"/TestSettings/Execution/Hosts");
                testSettingsNodes.Execution = testSettingsRoot.SelectSingleNode(@"/TestSettings/Execution");

                if (testSettingsNodes.Timeout != null && (testSettingsNodes.Timeout.Attributes[TestTimeoutAttributeName] != null ||
                    testSettingsNodes.Timeout.Attributes[TestTimeoutAttributeName] != null || testSettingsNodes.Timeout.Attributes[TestTimeoutAttributeName] != null))
                {
                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ValidUsage));
                }
            }

            return testSettingsNodes;
        }

        /// <summary>
        /// Removes the embedded testSettings node if present.
        /// </summary>
        /// <param name="newXmlDoc"></param>
        private void RemoveEmbeddedTestSettings(XmlDocument newXmlDoc)
        {
            var testSettingsNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");
            if (testSettingsNode != null)
            {
                testSettingsNode.ParentNode.RemoveChild(testSettingsNode);
            }
        }

        /// <summary>
        /// Adds the legacy nodes to runSettings xml.
        /// </summary>
        /// <param name="deploymentNode"></param>
        /// <param name="scriptnode"></param>
        /// <param name="testTimeout"></param>
        /// <param name="assemblyresolutionNode"></param>
        /// <param name="hostsNode"></param>
        /// <param name="parallelTestCount"></param>
        /// <param name="newXmlDoc"></param>
        private void AddLegacyNodes(TestSettingsNodes testSettingsNodes, string testTimeout, string parallelTestCount, XmlDocument newXmlDoc)
        {
            //Remove if the legacy node already exists.
            var legacyNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/LegacySettings");
            if (legacyNode != null)
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.IgnoringLegacySettings));
                legacyNode.ParentNode.RemoveChild(legacyNode);
            }

            legacyNode = newXmlDoc.CreateNode(XmlNodeType.Element, LegacySettingsNodeName, null);

            if (testSettingsNodes.Deployment != null)
            {
                legacyNode.AppendChild(newXmlDoc.ImportNode(testSettingsNodes.Deployment, deep: true));
            }
            if (testSettingsNodes.Script != null)
            {
                legacyNode.AppendChild(newXmlDoc.ImportNode(testSettingsNodes.Script, deep: true));
            }

            // Execution node.
            if (testSettingsNodes.UnitTestConfig != null || !string.IsNullOrEmpty(parallelTestCount) || !string.IsNullOrEmpty(testTimeout) || testSettingsNodes.Hosts != null)
            {
                var newExecutionNode = newXmlDoc.CreateNode(XmlNodeType.Element, ExecutionNodeName, null);

                if (string.IsNullOrEmpty(parallelTestCount))
                {
                    var paralellAttribute = newXmlDoc.CreateAttribute(ParallelTestCountAttributeName);
                    paralellAttribute.Value = parallelTestCount;
                    newExecutionNode.Attributes.Append(paralellAttribute);
                }
                if (!string.IsNullOrEmpty(testTimeout))
                {
                    var newTimeoutsNode = newXmlDoc.CreateNode(XmlNodeType.Element, TimeoutsNodeName, null);
                    var testtimeoutattribute = newXmlDoc.CreateAttribute(TestTimeoutAttributeName);
                    testtimeoutattribute.Value = testTimeout;
                    newTimeoutsNode.Attributes.Append(testtimeoutattribute);
                    newExecutionNode.AppendChild(newXmlDoc.ImportNode(newTimeoutsNode, deep: true));
                }
                if (testSettingsNodes.Hosts != null)
                {
                    newExecutionNode.AppendChild(newXmlDoc.ImportNode(testSettingsNodes.Hosts, deep: true));
                }
                if (testSettingsNodes.UnitTestConfig != null)
                {
                    var testTypeSpecificNode = newXmlDoc.CreateNode(XmlNodeType.Element, TestTypeSpecificNodeName, null);
                    testTypeSpecificNode.AppendChild(newXmlDoc.ImportNode(testSettingsNodes.UnitTestConfig, deep: true));
                    newExecutionNode.AppendChild(newXmlDoc.ImportNode(testTypeSpecificNode, deep: true));
                }
                legacyNode.AppendChild(newXmlDoc.ImportNode(newExecutionNode, deep: true));
            }
            newXmlDoc.DocumentElement.AppendChild(legacyNode);
        }

        /// <summary>
        /// Adds the datacollector nodes to the runSettings xml.
        /// </summary>
        /// <param name="oldDatacollectorNodes"></param>
        /// <param name="newXmlDoc"></param>
        private void AddDataCollectorNodes(XmlNodeList oldDatacollectorNodes, XmlDocument newXmlDoc)
        {
            var dataCollectionRunSettingsNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings");
            if (dataCollectionRunSettingsNode == null)
            {
                dataCollectionRunSettingsNode = newXmlDoc.CreateNode(XmlNodeType.Element, DataCollectionRunSettingsNodeName, null);
            }

            var dataCollectorsNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings/DataCollectors");
            if (dataCollectorsNode == null)
            {
                dataCollectorsNode = newXmlDoc.CreateNode(XmlNodeType.Element, DataCollectorsNodeName, null);
                dataCollectionRunSettingsNode.AppendChild(newXmlDoc.ImportNode(dataCollectorsNode, deep: true));
                dataCollectorsNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings/DataCollectors");
            }

            foreach (XmlNode datacollector in oldDatacollectorNodes)
            {
                dataCollectorsNode.AppendChild(newXmlDoc.ImportNode(datacollector, deep: true));
            }
            newXmlDoc.DocumentElement.AppendChild(dataCollectionRunSettingsNode);
        }

        /// <summary>
        /// Adds run session timeout node.
        /// </summary>
        /// <param name="runTimeout"></param>
        /// <param name="newXmlDoc"></param>
        private void AddRunTimeoutNode(string runTimeout, XmlDocument newXmlDoc)
        {
            var runConfigurationNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/RunConfiguration");
            if (runConfigurationNode == null)
            {
                runConfigurationNode = newXmlDoc.CreateNode(XmlNodeType.Element, RunConfigurationNodeName, null);
            }

            var testSessionTimeoutNode = newXmlDoc.CreateNode(XmlNodeType.Element, TestSessionTimeoutNodeName, null);
            testSessionTimeoutNode.InnerText = runTimeout;
            runConfigurationNode.AppendChild(newXmlDoc.ImportNode(testSessionTimeoutNode, deep: true));

            newXmlDoc.DocumentElement.AppendChild(runConfigurationNode);
        }

        const string TestTimeoutAttributeName = "testTimeout";
        const string ParallelTestCountAttributeName = "testTimeout";
        const string RunTimeoutAttributeName = "runTimeout";
        const string LegacySettingsNodeName = "LegacySettings";
        const string ExecutionNodeName = "Execution";
        const string TimeoutsNodeName = "Timeouts";
        const string TestTypeSpecificNodeName = "TestTypeSpecific";
        const string RunConfigurationNodeName = "RunConfiguration";
        const string TestSessionTimeoutNodeName = "TestSessionTimeout";
        const string DataCollectionRunSettingsNodeName = "DataCollectionRunSettings";
        const string DataCollectorsNodeName = "DataCollectors";
        const string sampleRunSettingsContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                                "<RunSettings></RunSettings>";
        const string agentNotRespondingTimeout = "agentNotRespondingTimeout";
        const string DeploymentTimeout = "deploymentTimeout";
        const string ScriptTimeout = "scriptTimeout";
    }
}

