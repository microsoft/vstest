// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Implementations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Utilities.Helpers.Interfaces;

    [TestClass]
    public class CommandLineOptionsTests
    {
        [TestMethod]
        public void CommandLineOptionsDefaultBatchSizeIsTen()
        {
            Assert.AreEqual(10, CommandLineOptions.Instance.BatchSize);
        }

        [TestMethod]
        public void CommandLineOptionsDefaultTestRunStatsEventTimeoutIsOnePointFiveSec()
        {
            var timeout = new TimeSpan(0, 0, 0, 1, 500);
            Assert.AreEqual(timeout, CommandLineOptions.Instance.TestRunStatsEventTimeout);
        }

        [TestMethod]
        public void CommandLineOptionsGetForSourcesPropertyShouldReturnReadonlySourcesEnumerable()
        {
            Assert.IsTrue(CommandLineOptions.Instance.Sources is ReadOnlyCollection<string>);
        }

        [TestMethod]
        public void CommandLineOptionsGetForHasPhoneContextPropertyIfTargetDeviceIsSetReturnsTrue()
        {
            Assert.IsFalse(CommandLineOptions.Instance.HasPhoneContext);
            // Set some not null value
            CommandLineOptions.Instance.TargetDevice = "TargetDevice";
            Assert.IsTrue(CommandLineOptions.Instance.HasPhoneContext);
        }
        
        [TestMethod]
        public void CommandLineOptionsAddSourceShouldThrowCommandLineExceptionForNullSource()
        {
            try
            {
                CommandLineOptions.Instance.AddSource(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("Cannot be null or empty", ex.Message);
            }
        }
        
        [TestMethod]
        public void CommandLineOptionsAddSourceShouldThrowCommandLineExceptionForInvalidSource()
        {
            try
            {
                CommandLineOptions.Instance.AddSource("DummySource");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The test source file \"DummySource\" provided was not found.", ex.Message);
            }
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceThrowExceptionIfDuplicateSource()
        {
            var testFilePath = "DummyTestFile.txt";
            
            // Setup mocks.
            var mockFileHelper = new MockFileHelper();
            mockFileHelper.ExistsInvoker = (path) =>
            {
                if (string.Equals(path, testFilePath))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };

            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = mockFileHelper;
            CommandLineOptions.Instance.AddSource(testFilePath);
            
            var isExceptionThrown = false;
            try
            {
                // Trying to add duplicate source
                CommandLineOptions.Instance.AddSource(testFilePath);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("Duplicate source " + testFilePath + " specified.", ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceForValidSource()
        {
            string testFilePath = "DummyTestFile.txt";
            
            // Setup mocks.
            var mockFileHelper = new MockFileHelper();
            mockFileHelper.ExistsInvoker = (path) =>
            {
                if (string.Equals(path, testFilePath))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };

            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = mockFileHelper;
            CommandLineOptions.Instance.AddSource(testFilePath);
            
            // Check if the testsource is present in the TestSources
            Assert.IsTrue(CommandLineOptions.Instance.Sources.Contains(testFilePath));
        }
    }
}
