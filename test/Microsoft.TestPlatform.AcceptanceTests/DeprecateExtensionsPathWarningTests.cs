namespace Microsoft.TestPlatform.AcceptanceTests
{
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
                foreach (var file in copiedFiles)
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
            copiedFiles = new List<string>();
            var extensionsDir = Path.Combine(Path.GetDirectoryName(GetConsoleRunnerPath()), "Extensions");
            adapterDependencies = Directory.GetFiles(GetTestAdapterPath(), "*.dll", SearchOption.TopDirectoryOnly);

            try
            {
                foreach (var file in adapterDependencies)
                {
                    var fileCopied = Path.Combine(extensionsDir, Path.GetFileName(file));
                    copiedFiles.Add(fileCopied);
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
            var arguments = PrepareArguments(GetSampleTestAssembly(), null, null, FrameworkArgValue, resultsDirectory: resultsDir);

            InvokeVsTest(arguments);
            StdOutputContains("Adapter lookup is being changed, please follow");

            TryRemoveDirectory(resultsDir);
        }

        public override string GetConsoleRunnerPath()
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(typeof(DeprecateExtensionsPathWarningTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.Parent.Parent.Parent;

            return Path.Combine(currentDirectory.FullName, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
        }
    }
}