using System;
using System.IO;
using System.Xml;

namespace SettingsMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [Full path to testsettings file/runsettings file to be migrated] [Full path to new runsettings file]");
            }

            string oldFilePath = args[0];
            string newFilePath = args[1];

            if(!Path.IsPathRooted(oldFilePath) || !Path.IsPathRooted(newFilePath) || !string.Equals(Path.GetExtension(newFilePath), RunsettingsExtension))
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [Full path to testsettings file/runsettings file to be migrated] [Full path to new runsettings file]");
            }

            if(string.Equals(Path.GetExtension(oldFilePath), TestsettingsExtension))
            {
                MigrateTestsettings(oldFilePath, newFilePath, runsettingsContent);
            }
            else if(string.Equals(Path.GetExtension(oldFilePath), RunsettingsExtension))
            {
                MigrateRunsettings(oldFilePath, newFilePath);
            }
            else
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [Full path to testsettings file/runsettings file to be migrated] [Full path to new runsettings file]");
            }
        }

        static void MigrateRunsettings(string oldRunsettingsPath, string newRunsettingsPath)
        {
            string testsettingsPath = null;
            using (XmlTextReader reader = new XmlTextReader(oldRunsettingsPath))
            {
                reader.Namespaces = false;

                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;

                var testsettingsNode = root.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");
                if(testsettingsNode != null)
                {
                    testsettingsPath = testsettingsNode.InnerText;
                }
                if(!string.IsNullOrWhiteSpace(testsettingsPath))
                {
                    if(!Path.IsPathRooted(testsettingsPath))
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

        static void MigrateTestsettings(string testsettingsPath, string newRunsettingsPath, string oldRunSettingsContent)
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

                var hasTestTimeout = timeoutNode != null && timeoutNode.Attributes[TestTimeoutAttributeName] != null;
                var hasRunTimeout = timeoutNode != null && timeoutNode.Attributes[RunTimeoutAttributeName] != null;
                var hasParallelTestCount = executionNode != null && executionNode.Attributes[ParallelTestCountAttributeName] != null;

                var newxml = new XmlDocument();
                newxml.LoadXml(oldRunSettingsContent);

                if (websettingsNode != null)
                {
                    newxml.DocumentElement.AppendChild(newxml.ImportNode(websettingsNode, deep: true));
                }

                if (deploymentNode != null || scriptnode != null || assemblyresolutionNode != null ||
                    hasParallelTestCount || hasTestTimeout || hostsNode != null)
                {
                    //Remove if the legacy node already exists.
                    var legacyNode = newxml.DocumentElement.SelectSingleNode(@"/RunSettings/LegacySettings");
                    if (legacyNode != null)
                    {
                        legacyNode.ParentNode.RemoveChild(legacyNode);
                    }

                    legacyNode = newxml.CreateNode(XmlNodeType.Element, LegacySettingsNodeName, null);

                    if (deploymentNode != null)
                    {
                        legacyNode.AppendChild(newxml.ImportNode(deploymentNode, deep: true));
                    }
                    if (scriptnode != null)
                    {
                        legacyNode.AppendChild(newxml.ImportNode(scriptnode, deep: true));
                    }

                    if (assemblyresolutionNode != null || hasParallelTestCount || hasTestTimeout || hostsNode != null)
                    {
                        var newExecutionNode = newxml.CreateNode(XmlNodeType.Element, ExecutionNodeName, null);

                        if (hasParallelTestCount)
                        {
                            var paralellAttribute = newxml.CreateAttribute(ParallelTestCountAttributeName);
                            paralellAttribute.Value = executionNode.Attributes[ParallelTestCountAttributeName].Value;
                            newExecutionNode.Attributes.Append(paralellAttribute);
                        }

                        if (hasTestTimeout)
                        {
                            var newTimeoutsNode = newxml.CreateNode(XmlNodeType.Element, TimeoutsNodeName, null);
                            var testtimeoutattribute = newxml.CreateAttribute(TestTimeoutAttributeName);
                            testtimeoutattribute.Value = timeoutNode.Attributes[TestTimeoutAttributeName].Value;
                            newTimeoutsNode.Attributes.Append(testtimeoutattribute);
                            newExecutionNode.AppendChild(newxml.ImportNode(newTimeoutsNode, deep: true));
                        }

                        if (hostsNode != null)
                        {
                            newExecutionNode.AppendChild(newxml.ImportNode(hostsNode, deep: true));
                        }

                        if (assemblyresolutionNode != null)
                        {
                            var testTypeSpecificNode = newxml.CreateNode(XmlNodeType.Element, TestTypeSpecificNodeName, null);
                            testTypeSpecificNode.AppendChild(newxml.ImportNode(assemblyresolutionNode, deep: true));
                            newExecutionNode.AppendChild(newxml.ImportNode(testTypeSpecificNode, deep: true));
                        }

                        legacyNode.AppendChild(newxml.ImportNode(newExecutionNode, deep: true));
                    }

                    newxml.DocumentElement.AppendChild(legacyNode);
                }


                if (hasRunTimeout)
                {
                    var runConfigurationNode = newxml.DocumentElement.SelectSingleNode(@"/RunSettings/RunConfiguration");
                    if(runConfigurationNode == null)
                    {
                        runConfigurationNode = newxml.CreateNode(XmlNodeType.Element, RunConfigurationNodeName, null);
                    }
                    
                    var testSessionTimeoutNode = newxml.CreateNode(XmlNodeType.Element, TestSessionTimeoutNodeName, null);
                    testSessionTimeoutNode.InnerText = timeoutNode.Attributes[RunTimeoutAttributeName].Value;
                    runConfigurationNode.AppendChild(newxml.ImportNode(testSessionTimeoutNode, deep: true));

                    newxml.DocumentElement.AppendChild(runConfigurationNode);
                }

                if (oldDatacollectorNodes != null && oldDatacollectorNodes.Count > 0)
                {

                    var dataCollectionRunSettingsNode = newxml.DocumentElement.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings");
                    if(dataCollectionRunSettingsNode == null)
                    {
                        dataCollectionRunSettingsNode = newxml.CreateNode(XmlNodeType.Element, DataCollectionRunSettingsNodeName, null);
                    }
                    
                    var dataCollectorsNode = newxml.DocumentElement.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings/DataCollectors");
                    if(dataCollectorsNode == null)
                    {
                        dataCollectorsNode = newxml.CreateNode(XmlNodeType.Element, DataCollectorsNodeName, null);
                        dataCollectionRunSettingsNode.AppendChild(newxml.ImportNode(dataCollectorsNode, deep: true));
                        dataCollectorsNode = newxml.DocumentElement.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings/DataCollectors");
                    }

                    foreach(XmlNode datacollector in oldDatacollectorNodes)
                    {
                        dataCollectorsNode.AppendChild(newxml.ImportNode(datacollector, deep: true));
                    }
                    newxml.DocumentElement.AppendChild(dataCollectionRunSettingsNode);
                }


                newxml.Save(newRunsettingsPath);
            }
        }


        const string runsettingsContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                          "<RunSettings></RunSettings>";
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
        const string RunsettingsExtension = ".runsettings";
        const string TestsettingsExtension = ".testsettings";
    }
}

