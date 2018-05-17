// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests
{
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MigratorUnitTests
    {
        [TestMethod]
        public void MigratorGeneratesCorrectRunsettingsForEmbeddedTestSettings()
        {
            var migrator = new Migrator();
            string newRunsettingsPath = Path.Combine(Path.GetTempPath(), "generatedRunsettings.runsettings");
            string oldTestsettingsPath = Path.Combine(Path.GetTempPath(), "oldTestsettings.testsettings");
            string oldRunsettingsPath = Path.Combine(Path.GetTempPath(), "oldRunsettings.runsettings");
            
            File.WriteAllText(oldTestsettingsPath, OldTestSettings);
            File.WriteAllText(newRunsettingsPath, "");

            var doc = new XmlDocument();
            doc.LoadXml(OldRunSettings);
            var settingsnode = doc.DocumentElement.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");
                settingsnode.InnerText = oldTestsettingsPath;
            File.WriteAllText(oldRunsettingsPath, doc.InnerXml);

            migrator.MigrateRunSettings(oldRunsettingsPath, newRunsettingsPath);

            Assert.IsTrue(File.Exists(newRunsettingsPath), "Run settings should be generated.");

            using (XmlTextReader reader = new XmlTextReader(newRunsettingsPath))
            {
                reader.Namespaces = false;

                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;

                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/WebTestRunConfiguration/Browser/Headers/Header"), "There should be a WebTestRunConfiguration node");
                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings"), "There should be a LegacySettings node");
                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings/Deployment/DeploymentItem"), "There should be a DeploymentItem node");

                var scriptNode = root.SelectSingleNode(@"/RunSettings/LegacySettings/Scripts");
                Assert.IsNotNull(scriptNode, "There should be a WebTestRunConfiguration node");
                Assert.AreEqual(".\\setup.bat", scriptNode.Attributes["setupScript"].Value, "setupScript does not match.");
                Assert.AreEqual(".\\cleanup.bat", scriptNode.Attributes["cleanupScript"].Value, "cleanupScript does not match.");

                var timeoutNode = root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/Timeouts");
                Assert.IsNotNull(timeoutNode, "There should be a Timeouts node");
                Assert.AreEqual("120000", timeoutNode.Attributes["testTimeout"].Value, "testTimeout value does not match.");

                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/TestTypeSpecific/UnitTestRunConfig/AssemblyResolution/TestDirectory"), "There should be a Assembly resolution node");

                var testSessionTimeoutNode = root.SelectSingleNode(@"/RunSettings/RunConfiguration/TestSessionTimeout");
                Assert.IsNotNull(testSessionTimeoutNode, "There should be a TestSessionTimeout node");
                Assert.AreEqual(testSessionTimeoutNode.InnerText, "60000", "Timeout value does not match.");
            }

            File.Delete(oldRunsettingsPath);
            File.Delete(newRunsettingsPath);
            File.Delete(oldTestsettingsPath);
        }


        [TestMethod]
        public void MigratorGeneratesCorrectRunsettingsForTestSettings()
        {
            var migrator = new Migrator();
            string newRunsettingsPath = Path.Combine(Path.GetTempPath(), "generatedRunsettings.runsettings");
            string oldTestsettingsPath = Path.Combine(Path.GetTempPath(), "oldTestsettings.testsettings");
            
            File.WriteAllText(oldTestsettingsPath, OldTestSettings);
            File.WriteAllText(newRunsettingsPath, "");

            migrator.MigrateTestSettings(oldTestsettingsPath, newRunsettingsPath);

            Assert.IsTrue(File.Exists(newRunsettingsPath), "Run settings should be generated.");

            using (XmlTextReader reader = new XmlTextReader(newRunsettingsPath))
            {
                reader.Namespaces = false;

                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;

                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/WebTestRunConfiguration/Browser/Headers/Header"), "There should be a WebTestRunConfiguration node");
                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings"), "There should be a LegacySettings node");
                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings/Deployment/DeploymentItem"), "There should be a DeploymentItem node");

                var scriptNode = root.SelectSingleNode(@"/RunSettings/LegacySettings/Scripts");
                Assert.IsNotNull(scriptNode, "There should be a WebTestRunConfiguration node");
                Assert.AreEqual(".\\setup.bat", scriptNode.Attributes["setupScript"].Value, "setupScript does not match.");
                Assert.AreEqual(".\\cleanup.bat", scriptNode.Attributes["cleanupScript"].Value, "cleanupScript does not match.");

                var timeoutNode = root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/Timeouts");
                Assert.IsNotNull(timeoutNode, "There should be a Timeouts node");
                Assert.AreEqual("120000", timeoutNode.Attributes["testTimeout"].Value, "testTimeout value does not match.");

                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/TestTypeSpecific/UnitTestRunConfig/AssemblyResolution/TestDirectory"), "There should be a Assembly resolution node");

                var testSessionTimeoutNode = root.SelectSingleNode(@"/RunSettings/RunConfiguration/TestSessionTimeout");
                Assert.IsNotNull(testSessionTimeoutNode, "There should be a TestSessionTimeout node");
                Assert.AreEqual(testSessionTimeoutNode.InnerText, "60000", "Timeout value does not match.");
            }

            File.Delete(newRunsettingsPath);
            File.Delete(oldTestsettingsPath);
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void InvalidSettingsThrowsException()
        {
            var migrator = new Migrator();
            string newRunsettingsPath = Path.Combine(Path.GetTempPath(), "generatedRunsettings.runsettings");
            string oldTestsettingsPath = Path.Combine(Path.GetTempPath(), "oldTestsettings.testsettings");
            
            File.WriteAllText(oldTestsettingsPath, InvalidSettings);
            File.WriteAllText(newRunsettingsPath, "");

            migrator.MigrateTestSettings(oldTestsettingsPath, newRunsettingsPath);

            File.Delete(oldTestsettingsPath);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void InvalidPathThrowsException()
        {
            var migrator = new Migrator();
            string newRunsettingsPath = @"X:\generatedRunsettings.runsettings";
            string oldTestsettingsPath = Path.Combine(Path.GetTempPath(), "oldTestsettings.testsettings");

            File.WriteAllText(oldTestsettingsPath, OldTestSettings);
            File.WriteAllText(newRunsettingsPath, "");

            migrator.MigrateTestSettings(oldTestsettingsPath, newRunsettingsPath);

            File.Delete(oldTestsettingsPath);
        }

        const string InvalidSettings = "<InvalidSettings>";
        const string OldRunSettings = "<RunSettings>" +
                                    "<MSTest>" +
                                    "<ForcedLegacyMode>true</ForcedLegacyMode>" +
                                    "<SettingsFile ></SettingsFile>" +
                                    "</MSTest>" +
                                    "</RunSettings>";

        const string OldTestSettings = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                    "<TestSettings name=\"TestSettings1\" id=\"cfb5c9a7-f57f-42db-8006-108cdf34bee1\" xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">" +
                                    "<Description>These are default test settings for a local test run.</Description>" +
                                    "<Deployment>" +
                                    "<DeploymentItem filename=\".\test.txt\" />" +
                                    "</Deployment>" +
                                    "<Scripts setupScript=\".\\setup.bat\" cleanupScript=\".\\cleanup.bat\" />" +
                                    "<Execution hostProcessPlatform=\"MSIL\">" +
                                    "<Timeouts runTimeout=\"60000\" testTimeout=\"120000\" />" +
                                    "<TestTypeSpecific>" +
                                    "<UnitTestRunConfig testTypeId=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\">" +
                                    "<AssemblyResolution>" +
                                    "<TestDirectory useLoadContext=\"true\" />" +
                                    "</AssemblyResolution>" +
                                    "</UnitTestRunConfig>" +
                                    "<WebTestRunConfiguration testTypeId=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\">" +
                                    "<Browser name=\"Internet Explorer 9.0\" MaxConnections=\"6\">" +
                                    "<Headers>" +
                                    "<Header name=\"User-Agent\" value=\"Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)\"/>" +
                                    "<Header name=\"Accept\" value=\"*/*\" />" +
                                    "<Header name=\"Accept-Language\" value=\"{{$IEAcceptLanguage}}\" />" +
                                    "<Header name=\"Accept-Encoding\" value=\"GZIP\" />" +
                                    "</Headers>" +
                                    "</Browser>" +
                                    "</WebTestRunConfiguration>" +
                                    "</TestTypeSpecific>" +
                                    "<AgentRule name=\"LocalMachineDefaultRole\">" +
                                    "</AgentRule>" +
                                    "</Execution>" +
                                    "<Properties/>" +
                                    "</TestSettings>";
    }   
}
