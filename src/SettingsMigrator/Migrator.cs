// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;

namespace SettingsMigrator
{
    public class Migrator
    {
        /// <summary>
        /// Given a runsettings with an embedded testsettings, converts it to runsettings.
        /// </summary>
        /// <param name="oldRunsettingsPath"></param>
        /// <param name="newRunsettingsPath"></param>
        public void MigrateRunsettings(string oldRunsettingsPath, string newRunsettingsPath)
        {
            string testsettingsPath = null;
            using (XmlTextReader reader = new XmlTextReader(oldRunsettingsPath))
            {
                reader.Namespaces = false;

                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;

                var testsettingsNode = root.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");

                if (testsettingsNode != null)
                {
                    testsettingsPath = testsettingsNode.InnerText;
                }
                if (!string.IsNullOrWhiteSpace(testsettingsPath))
                {
                    if (!Path.IsPathRooted(testsettingsPath))
                    {
                        testsettingsPath = Path.Combine(oldRunsettingsPath, testsettingsPath);
                    }

                    MigrateTestsettings(testsettingsPath, newRunsettingsPath, File.ReadAllText(oldRunsettingsPath));
                }
                else
                {
                    Console.WriteLine("Runsettings does not contain an embedded testsettings, not migrating.");
                }
            }
        }

        /// <summary>
        /// Given a testsettings, converts it to runsettings
        /// </summary>
        /// <param name="testsettingsPath"></param>
        /// <param name="newRunsettingsPath"></param>
        /// <param name="oldRunSettingsContent"></param>
        public void MigrateTestsettings(string testsettingsPath, string newRunsettingsPath, string oldRunSettingsContent)
        {
            using (XmlTextReader reader = new XmlTextReader(testsettingsPath))
            {
                reader.Namespaces = false;

                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;

                var deploymentNode = root.SelectSingleNode(@"/TestSettings/Deployment");
                var scriptnode = root.SelectSingleNode(@"/TestSettings/Scripts");
                var websettingsNode = root.SelectSingleNode(@"/TestSettings/Execution/TestTypeSpecific/WebTestRunConfiguration");
                var oldDatacollectorNodes = root.SelectNodes(@"/TestSettings/AgentRule/DataCollectors/DataCollector");
                var timeoutNode = root.SelectSingleNode(@"/TestSettings/Execution/Timeouts");
                var assemblyresolutionNode = root.SelectSingleNode(@"/TestSettings/Execution/TestTypeSpecific/UnitTestRunConfig");
                var hostsNode = root.SelectSingleNode(@"/TestSettings/Execution/Hosts");
                var executionNode = root.SelectSingleNode(@"/TestSettings/Execution");

                string testTimeout = null;
                if (timeoutNode != null && timeoutNode.Attributes[TestTimeoutAttributeName] != null)
                {
                    testTimeout = timeoutNode.Attributes[TestTimeoutAttributeName].Value;
                }

                string runTimeout = null;
                if (timeoutNode != null && timeoutNode.Attributes[RunTimeoutAttributeName] != null)
                {
                    runTimeout = timeoutNode.Attributes[RunTimeoutAttributeName].Value;
                }

                string parallelTestCount = null;
                if (executionNode != null && executionNode.Attributes[ParallelTestCountAttributeName] != null)
                {
                    parallelTestCount = executionNode.Attributes[ParallelTestCountAttributeName].Value;
                }

                var newXmlDoc = new XmlDocument();
                newXmlDoc.LoadXml(oldRunSettingsContent);

                // Remove the embedded testsettings node if it exists.
                var testsettingsNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");
                if (testsettingsNode != null)
                {
                    testsettingsNode.ParentNode.RemoveChild(testsettingsNode);
                }

                if (websettingsNode != null)
                {
                    newXmlDoc.DocumentElement.AppendChild(newXmlDoc.ImportNode(websettingsNode, deep: true));
                }

                if (deploymentNode != null || scriptnode != null || assemblyresolutionNode != null ||
                    !string.IsNullOrEmpty(parallelTestCount) || !string.IsNullOrEmpty(testTimeout) || hostsNode != null)
                {
                    AddLegacyNodes(deploymentNode, scriptnode, testTimeout, assemblyresolutionNode, hostsNode, parallelTestCount, newXmlDoc);
                }

                if (!string.IsNullOrEmpty(runTimeout))
                {
                    AddRunTimeoutNode(runTimeout, newXmlDoc);
                }

                if (oldDatacollectorNodes != null && oldDatacollectorNodes.Count > 0)
                {
                    AddDataCollectorNodes(oldDatacollectorNodes, newXmlDoc);
                }

                newXmlDoc.Save(newRunsettingsPath);
            }
        }

        private static void AddLegacyNodes(XmlNode deploymentNode, XmlNode scriptnode, string testTimeout, XmlNode assemblyresolutionNode, XmlNode hostsNode, string parallelTestCount, XmlDocument newXmlDoc)
        {
            //Remove if the legacy node already exists.
            var legacyNode = newXmlDoc.DocumentElement.SelectSingleNode(@"/RunSettings/LegacySettings");
            if (legacyNode != null)
            {
                legacyNode.ParentNode.RemoveChild(legacyNode);
            }

            legacyNode = newXmlDoc.CreateNode(XmlNodeType.Element, LegacySettingsNodeName, null);

            if (deploymentNode != null)
            {
                legacyNode.AppendChild(newXmlDoc.ImportNode(deploymentNode, deep: true));
            }
            if (scriptnode != null)
            {
                legacyNode.AppendChild(newXmlDoc.ImportNode(scriptnode, deep: true));
            }

            if (assemblyresolutionNode != null || !string.IsNullOrEmpty(parallelTestCount) || !string.IsNullOrEmpty(testTimeout) || hostsNode != null)
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

                if (hostsNode != null)
                {
                    newExecutionNode.AppendChild(newXmlDoc.ImportNode(hostsNode, deep: true));
                }

                if (assemblyresolutionNode != null)
                {
                    var testTypeSpecificNode = newXmlDoc.CreateNode(XmlNodeType.Element, TestTypeSpecificNodeName, null);
                    testTypeSpecificNode.AppendChild(newXmlDoc.ImportNode(assemblyresolutionNode, deep: true));
                    newExecutionNode.AppendChild(newXmlDoc.ImportNode(testTypeSpecificNode, deep: true));
                }

                legacyNode.AppendChild(newXmlDoc.ImportNode(newExecutionNode, deep: true));
            }

            newXmlDoc.DocumentElement.AppendChild(legacyNode);
        }

        private static void AddDataCollectorNodes(XmlNodeList oldDatacollectorNodes, XmlDocument newXmlDoc)
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

        private static void AddRunTimeoutNode(string runTimeout, XmlDocument newXmlDoc)
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
    }
}

