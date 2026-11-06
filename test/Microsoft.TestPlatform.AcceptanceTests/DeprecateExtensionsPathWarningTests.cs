namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [TestClass]
    [TestCategory("Windows-Review")]
    public class DeprecateExtensionsPathWarningTests : AcceptanceTestBase
    {
        private IList<string> adapterDependencies;
        private IList<string> copiedFiles;

        private string BuildConfiguration
        {
            get
            {
#if DEBUG
                return "Debug";
#else
                return "Release";
#endif
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                foreach (var file in this.copiedFiles)
                {
                    File.Delete(file);
                }
            }
            catch
            {

            }
        }

        [TestInitialize]
        public void CopyAdapterToExtensions()
        {
            this.testEnvironment.RunnerFramework = DesktopRunnerFramework;
            this.testEnvironment.TargetFramework = DesktopTargetFramework;
            this.copiedFiles = new List<string>();
            var extensionsDir = Path.Combine(Path.GetDirectoryName(this.GetConsoleRunnerPath()), "Extensions");
            this.adapterDependencies = Directory.GetFiles(this.GetTestAdapterPath(), "*.dll", SearchOption.TopDirectoryOnly);

            try
            {
                foreach (var file in this.adapterDependencies)
                {
                    var fileCopied = Path.Combine(extensionsDir, Path.GetFileName(file));
                    this.copiedFiles.Add(fileCopied);
                    File.Copy(file, fileCopied);
                }
            }
            catch
            {

            }
        }

        [TestMethod]
        public void VerifyDeprecatedWarningIsThrownWhenAdaptersPickedFromExtensionDirectory()
        {
            var resultsDir = GetResultsDirectory();
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), null, null, this.FrameworkArgValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.StdOutputContains("Adapter lookup is being changed, please follow");

            TryRemoveDirectory(resultsDir);
        }

        public override string GetConsoleRunnerPath()
        {
            // The repo root BY NAME. This read six .Parent calls from the test assembly's own
            // location, which is the same unnamed layout assumption as TelemetryPerfTestBase's three
            // - a different count in a different file encoding the same thing. Arcade inserts a
            // <ProjectName> segment into the output path, so any hard-coded level count is wrong by
            // one under it. This site has not fired yet only because the acceptance suite has not
            // run; it is the same defect waiting on a suite that has not executed.
            string root = IntegrationTestEnvironment.TestPlatformRootDirectory;

            return Path.Combine(root, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
        }
    }
}