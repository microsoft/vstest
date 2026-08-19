// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.Common.UnitTests.ExtensionFramework;

[TestClass]
public class TestPluginDiscovererTests
{
    private readonly List<TestRunMessageEventArgs> _messages = new();

    [TestInitialize]
    public void Initialize()
    {
        TestSessionMessageLogger.Instance.TestRunMessage += OnTestRunMessage;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // The logger is a process wide singleton, so drop the whole instance to make sure the handler
        // above does not observe messages from the tests that run after this one.
        TestSessionMessageLogger.Instance.TestRunMessage -= OnTestRunMessage;
        TestSessionMessageLogger.Instance = null;

        // So does the plugin cache, and one of the tests below clears it.
        TestPluginCacheHelper.ResetExtensionsCache();
    }

    private void OnTestRunMessage(object? sender, TestRunMessageEventArgs e) => _messages.Add(e);

    /// <summary>
    /// TestPluginDiscoverer remembers the files it failed on until the extension cache is cleared, so every
    /// test that wants to observe a failure needs a file name no other test has used.
    /// </summary>
    private static string GetPathOfMissingExtension()
        => Path.Combine(Path.GetTempPath(), $"missing{Guid.NewGuid():N}.TestAdapter.dll");

    private IEnumerable<TestRunMessageEventArgs> MessagesAbout(string file)
        => _messages.Where(m => m.Message.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0);

    [TestMethod]
    public void GetTestExtensionsInformationShouldNotThrowOnALoadException()
    {
        var pathToExtensions = new List<string> { "foo.dll" };

        // The below should not throw an exception.
        Assert.IsNotNull(TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldNotConsiderAbstractClasses()
    {
        var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).Assembly.Location };

        // The below should not throw an exception.
        var testExtensions = TestPluginDiscoverer.GetTestExtensionsInformation<TestDiscovererPluginInformation, ITestDiscoverer>(pathToExtensions);
        var discovererPluginInformation = new TestDiscovererPluginInformation(typeof(AbstractTestDiscoverer));
        Assert.IsFalse(testExtensions.ContainsKey(discovererPluginInformation.IdentifierData!));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldReturnDiscovererExtensions()
    {
        var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).Assembly.Location };

        // The below should not throw an exception.
        var testExtensions = TestPluginDiscoverer.GetTestExtensionsInformation<TestDiscovererPluginInformation, ITestDiscoverer>(pathToExtensions);

        var discovererPluginInformation = new TestDiscovererPluginInformation(typeof(ValidDiscoverer));
        var discovererPluginInformation2 = new TestDiscovererPluginInformation(typeof(ValidDiscoverer2));

        Assert.IsTrue(testExtensions.ContainsKey(discovererPluginInformation.IdentifierData!));
        Assert.IsTrue(testExtensions.ContainsKey(discovererPluginInformation2.IdentifierData!));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldReturnExecutorExtensions()
    {
        var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).Assembly.Location };

        // The below should not throw an exception.
        var testExtensions = TestPluginDiscoverer.GetTestExtensionsInformation<TestExecutorPluginInformation, ITestExecutor>(pathToExtensions);

        var pluginInformation = new TestExecutorPluginInformation(typeof(ValidExecutor));
        var pluginInformation2 = new TestExecutorPluginInformation(typeof(ValidExecutor2));

        Assert.AreEqual(2, testExtensions.Keys.Count(k => k.Contains("ValidExecutor")));
        Assert.IsTrue(testExtensions.ContainsKey(pluginInformation.IdentifierData!));
        Assert.IsTrue(testExtensions.ContainsKey(pluginInformation2.IdentifierData!));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldReturnLoggerExtensions()
    {
        var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).Assembly.Location };

        // The below should not throw an exception.
        var testExtensions = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions);

        var pluginInformation = new TestLoggerPluginInformation(typeof(ValidLogger));
        var pluginInformation2 = new TestLoggerPluginInformation(typeof(ValidLogger2));

        Assert.ContainsSingle(testExtensions.Keys.Where(k => k.Contains("csv")));
        Assert.IsTrue(testExtensions.ContainsKey(pluginInformation.IdentifierData!));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldReturnDataCollectorExtensionsAndIgnoresInvalidDataCollectors()
    {
        var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).Assembly.Location };

        // The below should not throw an exception.
        var testExtensions = TestPluginDiscoverer.GetTestExtensionsInformation<DataCollectorConfig, DataCollector>(pathToExtensions);

        var pluginInformation = new DataCollectorConfig(typeof(ValidDataCollector));

        Assert.HasCount(2, testExtensions.Keys);
        Assert.ContainsSingle(testExtensions.Keys.Where(k => k.Equals("datacollector://foo/bar")));
        Assert.ContainsSingle(testExtensions.Keys.Where(k => k.Equals("datacollector://foo/bar1")));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldReturnSettingsProviderExtensions()
    {
        var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).Assembly.Location };

        // The below should not throw an exception.
        var testExtensions = TestPluginDiscoverer.GetTestExtensionsInformation<TestSettingsProviderPluginInformation, ISettingsProvider>(pathToExtensions);

        var pluginInformation = new TestSettingsProviderPluginInformation(typeof(ValidSettingsProvider));
        var pluginInformation2 = new TestSettingsProviderPluginInformation(typeof(ValidSettingsProvider2));

        Assert.IsGreaterThanOrEqualTo(3, testExtensions.Keys.Select(k => k.Contains("ValidSettingsProvider")).Count());
        Assert.IsTrue(testExtensions.ContainsKey(pluginInformation.IdentifierData!));
        Assert.IsTrue(testExtensions.ContainsKey(pluginInformation2.IdentifierData!));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldNotAbortOnFaultyExtensions()
    {
        var pathToExtensions = new List<string>
        {
            typeof(TestPluginDiscovererTests).Assembly.Location,
        };

        _ = TestPluginDiscoverer.GetTestExtensionsInformation<FaultyTestExecutorPluginInformation, ITestExecutor>(pathToExtensions);

        _ = TestPluginDiscoverer.GetTestExtensionsInformation<FaultyTestExecutorPluginInformation, ITestExecutor>(pathToExtensions);
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnWhenAFileCannotBeLoaded()
    {
        var missingExtension = GetPathOfMissingExtension();

        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(new List<string> { missingExtension });

        var warning = _messages.SingleOrDefault(m => m.Level == TestMessageLevel.Warning && m.Message.Contains(missingExtension));
        Assert.IsNotNull(warning, $"Expected a warning naming '{missingExtension}', got: {string.Join(", ", _messages.Select(m => m.Message))}");
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnAboutTheSameFileOnlyOnce()
    {
        var missingExtension = GetPathOfMissingExtension();
        var pathToExtensions = new List<string> { missingExtension };

        // The same file is scanned once per extension type, the user should hear about it once.
        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions);
        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestDiscovererPluginInformation, ITestDiscoverer>(pathToExtensions);

        Assert.ContainsSingle(_messages.Where(m => m.Message.Contains(missingExtension)));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnAboutTheSameFileOnlyOnceWhenTheCasingDiffers()
    {
        var missingExtension = GetPathOfMissingExtension();

        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(
            new List<string> { missingExtension });
        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(
            new List<string> { missingExtension.ToUpperInvariant() });

        // On Windows those two paths are the same file, and a second warning about it tells the user nothing
        // they cannot already see in the first.
        Assert.ContainsSingle(MessagesAbout(missingExtension));
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnAgainAfterTheExtensionCacheIsCleared()
    {
        var missingExtension = GetPathOfMissingExtension();
        var pathToExtensions = new List<string> { missingExtension };

        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions);

        // This is what the runner does before every discovery or run request. Reporting once per run has to
        // mean once per run even in an editor that keeps the runner alive across many of them, otherwise the
        // user is told about a broken extension once and never again.
        TestPluginCache.Instance.ClearExtensions();

        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions);

        Assert.HasCount(2, MessagesAbout(missingExtension).ToList());
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldNotWarnAboutSpeculativelyProbedExtensions()
    {
        // With no extension paths the discoverer probes for the two C++ UWP adapters, which are missing
        // everywhere except UWP. Warning about those would put two warnings on every run.
        _ = TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(new List<string>());

        Assert.IsEmpty(_messages);
    }

    [TestMethod]
    public void GetTestExtensionsFromAssemblyShouldWarnAndKeepTheTypesThatLoadedOnReflectionTypeLoadException()
    {
        var filePath = GetPathOfMissingExtension();
        var assembly = new PartiallyLoadedAssembly(typeof(ValidDiscoverer), null);
        var pluginInfos = new Dictionary<string, TestDiscovererPluginInformation>();

        TestPluginDiscoverer.GetTestExtensionsFromAssembly<TestDiscovererPluginInformation, ITestDiscoverer>(assembly, pluginInfos, filePath);

        // The types that did load are still discovered, half an adapter is better than none.
        var expected = new TestDiscovererPluginInformation(typeof(ValidDiscoverer));
        Assert.IsTrue(pluginInfos.ContainsKey(expected.IdentifierData!));

        // And the user is told, instead of only finding out by re-running with /diag.
        var warning = _messages.SingleOrDefault(m => m.Level == TestMessageLevel.Warning && m.Message.Contains(filePath));
        Assert.IsNotNull(warning, $"Expected a warning naming '{filePath}', got: {string.Join(", ", _messages.Select(m => m.Message))}");
    }

    [TestMethod]
    public void GetTestExtensionsFromAssemblyShouldWarnOnceButKeepScanningTheAssembly()
    {
        var filePath = GetPathOfMissingExtension();
        var assembly = new PartiallyLoadedAssembly(typeof(ValidDiscoverer), null);
        var firstScan = new Dictionary<string, TestDiscovererPluginInformation>();
        var secondScan = new Dictionary<string, TestDiscovererPluginInformation>();

        TestPluginDiscoverer.GetTestExtensionsFromAssembly<TestDiscovererPluginInformation, ITestDiscoverer>(assembly, firstScan, filePath);
        TestPluginDiscoverer.GetTestExtensionsFromAssembly<TestDiscovererPluginInformation, ITestDiscoverer>(assembly, secondScan, filePath);

        Assert.ContainsSingle(_messages.Where(m => m.Message.Contains(filePath)));

        // Reporting once must not mean scanning once, the partially loaded assembly still has to be scanned
        // for every extension type.
        var expected = new TestDiscovererPluginInformation(typeof(ValidDiscoverer));
        Assert.IsTrue(secondScan.ContainsKey(expected.IdentifierData!));
    }

    #region Implementations

    /// <summary>
    /// An assembly that loaded but whose types did not, the way a real adapter behaves when one of its
    /// dependencies is missing. <see cref="ReflectionTypeLoadException.Types"/> holds null for every type
    /// that failed, which is what makes discovery silently return fewer extensions than the file declares.
    /// </summary>
    private sealed class PartiallyLoadedAssembly : Assembly
    {
        private readonly Type?[] _loadedTypes;

        public PartiallyLoadedAssembly(params Type?[] loadedTypes) => _loadedTypes = loadedTypes;

        public override string FullName => "PartiallyLoadedAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public override Type[] GetTypes()
            => throw new ReflectionTypeLoadException(
                _loadedTypes,
                new Exception[] { new FileNotFoundException("Could not load file or assembly 'Microsoft.Bcl.AsyncInterfaces, Version=9.0.0.8, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. The system cannot find the file specified.") });

        public override Type? GetType(string name, bool throwOnError, bool ignoreCase) => null;

        // These have to hand back an Attribute[], the reflection helpers cast the result back to one.
        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<Attribute>();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<Attribute>();

        public override bool IsDefined(Type attributeType, bool inherit) => false;
    }

    #region Discoverers

    private abstract class AbstractTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            throw new NotImplementedException();
        }
    }

    private class ValidDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            throw new NotImplementedException();
        }
    }

    private class ValidDiscoverer2 : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Executors

    [ExtensionUri("ValidExecutor")]
    private class ValidExecutor : ITestExecutor
    {
        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }
    }

    [ExtensionUri("ValidExecutor2")]
    private class ValidExecutor2 : ITestExecutor
    {
        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }
    }

    [ExtensionUri("ValidExecutor")]
    private class DuplicateExecutor : ITestExecutor
    {
        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Loggers

    [ExtensionUri("csv")]
    private class ValidLogger : ITestLogger
    {
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            throw new NotImplementedException();
        }
    }

    [ExtensionUri("docx")]
    private class ValidLogger2 : ITestLogger
    {
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            throw new NotImplementedException();
        }
    }

    [ExtensionUri("csv")]
    private class DuplicateLogger : ITestLogger
    {
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Settings Providers

    [SettingsName("ValidSettingsProvider")]
    private class ValidSettingsProvider : ISettingsProvider
    {
        public void Load(XmlReader reader)
        {
            throw new NotImplementedException();
        }
    }

    [SettingsName("ValidSettingsProvider2")]
    private class ValidSettingsProvider2 : ISettingsProvider
    {
        public void Load(XmlReader reader)
        {
            throw new NotImplementedException();
        }
    }

    [SettingsName("ValidSettingsProvider")]
    private class DuplicateSettingsProvider : ISettingsProvider
    {
        public void Load(XmlReader reader)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region  DataCollectors

    public class InvalidDataCollector : DataCollector
    {
        public override void Initialize(
            XmlElement? configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext? environmentContext)
        {
        }
    }

    /// <summary>
    /// The a data collector inheriting from another data collector.
    /// </summary>
    [DataCollectorFriendlyName("Foo1")]
    [DataCollectorTypeUri("datacollector://foo/bar1")]
    public class ADataCollectorInheritingFromAnotherDataCollector : InvalidDataCollector
    {
    }

    [DataCollectorFriendlyName("Foo")]
    [DataCollectorTypeUri("datacollector://foo/bar")]
    [DataCollectorAttachmentProcessor(typeof(DataCollectorAttachmentProcessor))]
    public class ValidDataCollector : DataCollector
    {
        public override void Initialize(
            XmlElement? configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext? environmentContext)
        {
        }
    }

    public class DataCollectorAttachmentProcessor : IDataCollectorAttachmentProcessor
    {
        public bool SupportsIncrementalProcessing => throw new NotImplementedException();

        public IEnumerable<Uri> GetExtensionUris()
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    internal class FaultyTestExecutorPluginInformation : TestExtensionPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="type"> The Type. </param>
        public FaultyTestExecutorPluginInformation(Type type) : base(type)
        {
            throw new Exception();
        }
    }
    #endregion
}
