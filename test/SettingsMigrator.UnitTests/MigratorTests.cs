// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.SettingsMigrator.UnitTests
{
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MigratorTests
    {
        private const string InvalidSettings = "<InvalidSettings>";

        private const string OldRunSettings = "<RunSettings>" +
                                    "<MSTest>" +
                                    "<ForcedLegacyMode>true</ForcedLegacyMode>" +
                                    "<SettingsFile></SettingsFile>" +
                                    "</MSTest>" +
                                    "</RunSettings>";

        private Migrator migrator;
        private string newRunsettingsPath;
        private string oldTestsettingsPath;
        private string oldRunsettingsPath;

        [TestInitialize]
        public void TestInit()
        {
            this.migrator = new Migrator();
            this.newRunsettingsPath = Path.Combine(Path.GetTempPath(), "generatedRunsettings.runsettings");
            this.oldTestsettingsPath = Path.GetFullPath(Path.Combine(".", "oldTestsettings.testsettings"));
            this.oldRunsettingsPath = Path.Combine(Path.GetTempPath(), "oldRunsettings.runsettings");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (File.Exists(this.newRunsettingsPath))
            {
                File.Delete(this.newRunsettingsPath);
            }
        }

        [TestMethod]
        public void NonRootedPathIsNotMigrated()
        {
            this.migrator.Migrate("asda", this.newRunsettingsPath);

            Assert.IsFalse(File.Exists(this.newRunsettingsPath), "Run settings should not be generated.");
        }

        [TestMethod]
        public void MigratorGeneratesCorrectRunsettingsForEmbeddedTestSettings()
        {
            var doc = new XmlDocument();
            doc.LoadXml(OldRunSettings);
            var settingsnode = doc.DocumentElement.SelectSingleNode(@"/RunSettings/MSTest/SettingsFile");
                settingsnode.InnerText = this.oldTestsettingsPath;
            File.WriteAllText(this.oldRunsettingsPath, doc.InnerXml);

            this.migrator.Migrate(this.oldRunsettingsPath, this.newRunsettingsPath);

            Validate(this.newRunsettingsPath);

            File.Delete(this.oldRunsettingsPath);
        }

        [TestMethod]
        public void MigratorGeneratesCorrectRunsettingsForEmbeddedTestSettingsOfRelativePath()
        {
            this.oldRunsettingsPath = Path.GetFullPath(Path.Combine(".", "oldRunSettingsWithEmbeddedSettings.runSEttings"));

            this.migrator.Migrate(this.oldRunsettingsPath, this.newRunsettingsPath);
            Validate(this.newRunsettingsPath);
        }

        [TestMethod]
        public void MigratorGeneratesCorrectRunsettingsWithDC()
        {
            this.oldRunsettingsPath = Path.GetFullPath(Path.Combine(".", "oldRunSettingsWithDataCollector.runsettings"));

            this.migrator.Migrate(this.oldRunsettingsPath, this.newRunsettingsPath);

            using (XmlTextReader reader = new XmlTextReader(this.newRunsettingsPath))
            {
                reader.Namespaces = false;
                var document = new XmlDocument();
                document.Load(reader);
                var root = document.DocumentElement;
                var dataCollectorNode = root.SelectNodes(@"/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector");
                Assert.AreEqual(2, dataCollectorNode.Count, "Data collector is missing");
            }
        }

        [TestMethod]
        public void MigratorGeneratesCorrectRunsettingsForTestSettings()
        {
            this.migrator.Migrate(this.oldTestsettingsPath, this.newRunsettingsPath);

            Validate(this.newRunsettingsPath);
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void InvalidSettingsThrowsException()
        {
            this.oldTestsettingsPath = Path.Combine(Path.GetTempPath(), "oldTestsettings.testsettings");

            File.WriteAllText(this.oldTestsettingsPath, InvalidSettings);
            File.WriteAllText(this.newRunsettingsPath, string.Empty);

            this.migrator.Migrate(this.oldTestsettingsPath, this.newRunsettingsPath);

            File.Delete(this.oldTestsettingsPath);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void InvalidPathThrowsException()
        {
            string oldTestsettingsPath = @"X:\generatedRun,settings.runsettings";

            this.migrator.Migrate(oldTestsettingsPath, this.newRunsettingsPath);
        }

        private static void Validate(string newRunsettingsPath)
        {
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

                var forcedLegacyNode = root.SelectSingleNode(@"/RunSettings/MSTest/ForcedLegacyMode");
                Assert.IsNotNull(forcedLegacyNode, "ForcedLegacy node should be present");
                Assert.AreEqual("true", forcedLegacyNode.InnerText, "Forced legacy should be true");

                var executionNode = root.SelectSingleNode(@" / RunSettings/LegacySettings/Execution");
                Assert.IsNotNull(executionNode, "There should be a Execution node");
                Assert.AreEqual("2", executionNode.Attributes["parallelTestCount"].Value, "parallelTestCount value does not match.");
                Assert.AreEqual("MSIL", executionNode.Attributes["hostProcessPlatform"].Value, "hostProcessPlatform value does not match.");

                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/Hosts"), "There should be a Hosts node");

                var timeoutNode = root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/Timeouts");
                Assert.IsNotNull(timeoutNode, "There should be a Timeouts node");
                Assert.AreEqual("120000", timeoutNode.Attributes["testTimeout"].Value, "testTimeout value does not match.");

                Assert.IsNotNull(root.SelectSingleNode(@"/RunSettings/LegacySettings/Execution/TestTypeSpecific/UnitTestRunConfig/AssemblyResolution/TestDirectory"), "There should be a Assembly resolution node");

                var testSessionTimeoutNode = root.SelectSingleNode(@"/RunSettings/RunConfiguration/TestSessionTimeout");
                Assert.IsNotNull(testSessionTimeoutNode, "There should be a TestSessionTimeout node");
                Assert.AreEqual("60000", testSessionTimeoutNode.InnerText, "Timeout value does not match.");

                var dataCollectorNode = root.SelectSingleNode(@"/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector");
                Assert.IsNotNull(dataCollectorNode, "There should be a DataCollector node");
                Assert.AreEqual("Event Log", dataCollectorNode.Attributes["friendlyName"].Value, "Data collector does not match.");
            }
        }
    }
}
