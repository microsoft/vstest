namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [TestClass]
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
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                null,
                null,
                this.FrameworkArgValue);

            this.InvokeVsTest(arguments);

            this.StdOutputContains("Adapter lookup is being changed, please follow");
        }

        public override string GetConsoleRunnerPath()
        {
            // Name the root instead of counting six directories up from the test assembly. The
            // count assumed the historic output layout; under Arcade the assembly sits one level
            // deeper, in artifacts\bin\<Project>\<config>\<tfm>, so the same six parents land
            // somewhere that is not the repository root and the path produced is not a location
            // anything creates.
            return Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
        }
    }
}