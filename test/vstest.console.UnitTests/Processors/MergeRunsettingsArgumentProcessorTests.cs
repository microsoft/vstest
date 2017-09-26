// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;
    using vstest.console.UnitTests.Processors;

    using ExceptionUtilities = Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.ExceptionUtilities;

    [TestClass]
    public class MergeRunsettingsArgumentProcessorTests
    {
        private MergeRunsettingsArgumentExecutor executor;
        private IRunSettingsProvider runSettingsProvider;
        private CommandLineOptions commandLineOptions;
        private Mock<IInferHelper> mockInferHelper;

        [TestInitialize]
        public void Init()
        {
            this.runSettingsProvider = new TestableRunSettingsProvider();
            this.commandLineOptions = CommandLineOptions.Instance;
            this.mockInferHelper = new Mock<IInferHelper>();
            this.executor = new MergeRunsettingsArgumentExecutor(
                commandLineOptions,
                this.runSettingsProvider,
                ConsoleOutput.Instance,
                mockInferHelper.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.commandLineOptions.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnMergeRunsettingsArgumentProcessorCapabilities()
        {
            var processor = new MergeRunsettingsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is MergeRunsettingsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecutorShouldReturnMergeRunsettingsArgumentExecutor()
        {
            var processor = new MergeRunsettingsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is MergeRunsettingsArgumentExecutor);
        }

        #region MergeRunsettingsArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new MergeRunsettingsArgumentProcessorCapabilities();
            Assert.AreEqual("/MergeRunsettings", capabilities.CommandName);

            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.MergeRunsettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(true, capabilities.AlwaysExecute);
            Assert.AreEqual(true, capabilities.IsSpecialCommand);
        }

        #endregion

        #region MergeRunsettingsArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldUpdateArchIfNotSpecified()
        {
            this.mockInferHelper.Setup(ih => ih.AutoDetectArchitecture(It.IsAny<List<string>>()))
                .Returns(Architecture.ARM);
            this.executor.Initialize(string.Empty);
            Assert.IsTrue(this.runSettingsProvider.ActiveRunSettings.SettingsXml.Contains(Architecture.ARM.ToString()));
            this.mockInferHelper.Verify(ih => ih.AutoDetectArchitecture(It.IsAny<List<string>>()));
        }

        [TestMethod]
        public void InitializeShouldNotUpdateArchIfSpecified()
        {
            this.commandLineOptions.TargetArchitecture = Architecture.X64;
            this.mockInferHelper.Setup(ih => ih.AutoDetectArchitecture(It.IsAny<List<string>>()))
                .Returns(Architecture.ARM);
            this.executor.Initialize(string.Empty);
            Assert.IsFalse(this.runSettingsProvider.ActiveRunSettings.SettingsXml.Contains(Architecture.ARM.ToString()));
            this.mockInferHelper.Verify(ih => ih.AutoDetectArchitecture(It.IsAny<List<string>>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldUpdateFrameworkIfNotSpecified()
        {
            this.mockInferHelper.Setup(ih => ih.AutoDetectFramework(It.IsAny<List<string>>()))
                .Returns(Framework.FromString(Constants.DotNetFramework46));
            this.executor.Initialize(string.Empty);
            Assert.IsTrue(this.runSettingsProvider.ActiveRunSettings.SettingsXml.Contains(Constants.DotNetFramework46));
            this.mockInferHelper.Verify(ih => ih.AutoDetectFramework(It.IsAny<List<string>>()));
        }

        [TestMethod]
        public void InitializeShouldNotUpdateFrameworkIfSpecified()
        {
            this.commandLineOptions.TargetFrameworkVersion = Framework.DefaultFramework;
            this.mockInferHelper.Setup(ih => ih.AutoDetectFramework(It.IsAny<List<string>>()))
                .Returns(Framework.FromString(Constants.DotNetFramework46));
            this.executor.Initialize(string.Empty);
            Assert.IsFalse(this.runSettingsProvider.ActiveRunSettings.SettingsXml.Contains(Constants.DotNetFramework46));
            this.mockInferHelper.Verify(ih => ih.AutoDetectFramework(It.IsAny<List<string>>()), Times.Never);
        }

        #endregion

        #region MergeRunsettingsArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            Assert.AreEqual(ArgumentProcessorResult.Success, this.executor.Execute());
        }

        #endregion

    }
}
