// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// The blame mode test host launcher tests.
    /// </summary>
    [TestClass]
    public class BlameModeTestHostLauncherTests
    {
        private Mock<IProcessHelper> mockProcessHelper;
        private TestableCrashDumpUtilities crashDumpUtilities;
        private Mock<IEnvironment> mockEnvironment;
        private Mock<IFileHelper> mockFileHelper;
        private Mock<IBlameDumpFolder> mockBlameDumpFolderGetter;
        private BlameModeTestHostLauncher blameModeTestHostLauncher;
        private string errorMessage;
        private int exitCode;

        public BlameModeTestHostLauncherTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockBlameDumpFolderGetter = new Mock<IBlameDumpFolder>();
            this.mockEnvironment = new Mock<IEnvironment>();
        }

        [TestMethod]
        public void LaunchTestHostShouldThrowExceptionIfTestHostStartInfIsNull()
        {
            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameModeTestHostLauncher.LaunchTestHost(null);
            });
        }

        [TestMethod]
        public void LaunchTestHostShouldReturnTestHostProcessId()
        {
            this.mockProcessHelper.Setup(
                ph =>
                    ph.LaunchProcess(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Action<object, string>>(),
                        It.IsAny<Action<object>>())).Returns(Process.GetCurrentProcess());
            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();

            int processId = this.blameModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(Process.GetCurrentProcess().Id, processId);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void ProcessExitedButNoErrorMessageIfNoDataWritten(int exitCode)
        {
            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.blameModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(this.errorMessage, string.Empty);
            Assert.AreEqual(this.exitCode, exitCode);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void ErrorReceivedCallbackShouldNotLogNullOrEmptyData(string errorData)
        {
            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();
            this.ErrorCallBackTestHelper(errorData, -1);

            this.blameModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(this.errorMessage, string.Empty);
        }

        [TestMethod]
        [DataRow(-1)]
        public void OnHostExitedShouldGetCrashDumpFileIfPlatformOSIsWindows(int exitCode)
        {
            this.crashDumpUtilities = new TestableCrashDumpUtilities(@"C:\dumpfile.mp", this.mockFileHelper.Object);
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            string dumpFolder;
            this.mockBlameDumpFolderGetter.Setup(x => x.GetCrashDumpFolderPath(It.IsAny<string>(), out dumpFolder)).Returns(true);

            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.blameModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(1, BlameLogger.GetDumpListCount());
        }

        [TestMethod]
        [DataRow(-1)]
        public void OnHostExitedShouldNotGetCrashDumpFileIfPlatformOSIsNotWindows(int exitCode)
        {
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.blameModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(0, BlameLogger.GetDumpListCount());
        }

        [TestMethod]
        [DataRow("", -1)]
        [DataRow(null, -1)]
        public void NullOrEmptyFileShouldNotBeAddedToDumpList(string filename, int exitCode)
        {
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

            this.crashDumpUtilities = new TestableCrashDumpUtilities(filename, this.mockFileHelper.Object);
            this.blameModeTestHostLauncher = this.GetBlameModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.blameModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(0, BlameLogger.GetDumpListCount());
        }

        [TestCleanup]
        public void CleanUp()
        {
            BlameLogger.ClearDumpList();
        }

        private TestableBlameModeTestHostLauncher GetBlameModeTestHostLauncher()
        {
            var launcher = new TestableBlameModeTestHostLauncher(
                this.mockProcessHelper.Object,
                this.crashDumpUtilities,
                this.mockEnvironment.Object,
                this.mockFileHelper.Object,
                this.mockBlameDumpFolderGetter.Object);

            return launcher;
        }

        private TestProcessStartInfo GetTestProcessStartInfo()
        {
            TestProcessStartInfo processInfo = new TestProcessStartInfo();
            processInfo.FileName = "C:\\Documents\\dotnet.exe";
            return processInfo;
        }

        private void BlameModeTestHostLauncherHostExited(object sender, HostProviderEventArgs e)
        {
            if (e.ErrroCode != 0)
            {
                this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        private void TestableBlameModeTestHostLauncherHostExited(object sender, HostProviderEventArgs e)
        {
            this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            this.exitCode = e.ErrroCode;
        }

        private void ErrorCallBackTestHelper(string errorMessage, int exitCode)
        {
            this.blameModeTestHostLauncher.HostExited += this.BlameModeTestHostLauncherHostExited;

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                    {
                        var process = Process.GetCurrentProcess();

                        errorCallback(process, errorMessage);
                        exitCallback(process);
                    }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
        }

        private void ExitCallBackTestHelper(int exitCode)
        {
            this.blameModeTestHostLauncher.HostExited += this.TestableBlameModeTestHostLauncherHostExited;

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                    {
                        var process = Process.GetCurrentProcess();
                        exitCallback(process);
                    }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
        }

        /// <summary>
        /// The testable blame mode test host launcher.
        /// </summary>
        private class TestableBlameModeTestHostLauncher : BlameModeTestHostLauncher
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableBlameModeTestHostLauncher"/> class.
            /// </summary>
            /// <param name="processHelper">Process Helper</param>
            /// <param name="crashDumpUtilities">Crash dump Utilities</param>
            /// <param name="environment">Environment</param>
            /// <param name="fileHelper">File Helper</param>
            /// <param name="blameDumpFolderGetter">Blame Dump Folder Getter</param>
            public TestableBlameModeTestHostLauncher(
                IProcessHelper processHelper,
                LocalCrashDumpUtilities crashDumpUtilities,
                IEnvironment environment,
                IFileHelper fileHelper,
                IBlameDumpFolder blameDumpFolderGetter)
                : base(processHelper, crashDumpUtilities, environment, fileHelper, blameDumpFolderGetter)
            {
                this.ErrorLength = 22;
            }
        }

        /// <summary>
        /// The testable crash dump utilities.
        /// </summary>
        private class TestableCrashDumpUtilities : LocalCrashDumpUtilities
        {
            private string filename;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestableCrashDumpUtilities"/> class.
            /// </summary>
            /// <param name="fileHelper">File Helper</param>
            /// <param name="filename">filename</param>
            public TestableCrashDumpUtilities(
                string filename,
                IFileHelper fileHelper)
                : base(fileHelper)
            {
                this.filename = filename;
            }

            public override string GetCrashDumpFile(string dumpPath, string applicationName, int processId)
            {
                return this.filename;
            }
        }
    }
}
