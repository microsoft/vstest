using System;
using System.Xml;

namespace SettingsMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [path to testsettings file] [path to runsettings file]");
            }

            Migrate(args[0], args[1]);
        }

        static void Migrate(string testsettings, string runsettings)
        {
            using (XmlTextReader reader = new XmlTextReader(testsettings))
            {
                reader.Namespaces = false;

                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;

                var deploymentNode = root.SelectSingleNode(@"/TestSettings/Deployment");
                var scriptnode = root.SelectSingleNode(@"/TestSettings/Scripts");
                var websettingsNode = root.SelectSingleNode(@"/TestSettings/Execution/TestTypeSpecific/WebTestRunConfiguration");
                var datacollectorsNode = root.SelectSingleNode(@"/TestSettings/AgentRule/DataCollectors");
                var timeoutNode = root.SelectSingleNode(@"/TestSettings/Execution/Timeouts");
                var assemblyresolutionNode = root.SelectSingleNode(@"/TestSettings/Execution/TestTypeSpecific/UnitTestRunConfig");
                var hostsNode = root.SelectSingleNode(@"/TestSettings/Execution/Hosts");
                var executionNode = root.SelectSingleNode(@"/TestSettings/Execution");

                var hasTestTimeout = timeoutNode != null && timeoutNode.Attributes[TestTimeoutAttributeName] != null;
                var hasRunTimeout = timeoutNode != null && timeoutNode.Attributes[RunTimeoutAttributeName] != null;
                var hasParallelTestCount = executionNode != null && executionNode.Attributes[ParallelTestCountAttributeName] != null;

                var newxml = new XmlDocument();
                newxml.LoadXml(runsettingsContent);

                if (websettingsNode != null)
                {
                    newxml.DocumentElement.AppendChild(newxml.ImportNode(websettingsNode, deep: true));
                }

                if (deploymentNode != null || scriptnode != null || assemblyresolutionNode != null ||
                    hasParallelTestCount || hasTestTimeout || hostsNode != null)
                {
                    var legacyNode = newxml.CreateNode(XmlNodeType.Element, LegacySettingsNodeName, null);

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
                    var runConfigurationNode = newxml.CreateNode(XmlNodeType.Element, RunConfigurationNodeName, null);
                    var testSessionTimeoutNode = newxml.CreateNode(XmlNodeType.Element, TestSessionTimeoutNodeName, null);
                    testSessionTimeoutNode.InnerText = timeoutNode.Attributes[RunTimeoutAttributeName].Value;
                    runConfigurationNode.AppendChild(newxml.ImportNode(testSessionTimeoutNode, deep: true));

                    newxml.DocumentElement.AppendChild(runConfigurationNode);
                }

                if (datacollectorsNode != null || hasRunTimeout)
                {
                    var dataCollectionRunSettingsNode = newxml.CreateNode(XmlNodeType.Element, DataCollectionRunSettingsNodeName, null);
                    if (datacollectorsNode != null)
                    {
                        dataCollectionRunSettingsNode.AppendChild(newxml.ImportNode(datacollectorsNode, deep: true));
                    }
                    newxml.DocumentElement.AppendChild(dataCollectionRunSettingsNode);
                }


                newxml.Save(runsettings);
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
    }
}

