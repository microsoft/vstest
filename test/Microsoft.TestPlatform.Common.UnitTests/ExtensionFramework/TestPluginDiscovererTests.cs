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
    [TestCleanup]
    public void Cleanup()
    {
        // The plugin cache is a process wide singleton and one of the tests below clears it.
        TestPluginCacheHelper.ResetExtensionsCache();
    }

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
    public void GetTestExtensionsInformationShouldWarnWhenAnExtensionCannotBeLoaded()
    {
        var extension = GetPathToExtensionThatCannotBeLoaded();

        var messages = CaptureSessionMessages(
            () => TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(new List<string> { extension }));

        var message = messages.Single();
        Assert.AreEqual(TestMessageLevel.Warning, message.Level);
        Assert.Contains(extension, message.Message);
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnOnlyOnceForTheSameExtension()
    {
        var extension = GetPathToExtensionThatCannotBeLoaded();

        var messages = CaptureSessionMessages(() =>
        {
            var paths = new List<string> { extension };

            // The same file is scanned once per extension type we look for, the user should hear about it once.
            TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(paths);
            TestPluginDiscoverer.GetTestExtensionsInformation<TestDiscovererPluginInformation, ITestDiscoverer>(paths);
            TestPluginDiscoverer.GetTestExtensionsInformation<TestExecutorPluginInformation, ITestExecutor>(paths);
        });

        Assert.ContainsSingle(messages);
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldNotWarnWhenProbingForKnownExtensions()
    {
        // With no extension paths we probe for a few well known extensions that are usually not present,
        // failing to load those is expected and must stay invisible to the user.
        var messages = CaptureSessionMessages(
            () => TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(new List<string>()));

        Assert.IsEmpty(messages);
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnAgainAfterTheExtensionCacheIsCleared()
    {
        var extension = GetPathToExtensionThatCannotBeLoaded();

        var messages = CaptureSessionMessages(() =>
        {
            var paths = new List<string> { extension };

            TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(paths);

            // This is what the runner does before every discovery or run request. Reporting once per run has to
            // mean once per run even in an editor that keeps the runner alive across many of them, otherwise the
            // user is told about a broken extension once and never again.
            TestPluginCache.Instance.ClearExtensions();

            TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(paths);
        });

        Assert.HasCount(2, messages);
    }

    [TestMethod]
    public void GetTestExtensionsInformationShouldWarnOnlyOnceForTheSameExtensionWhenTheCasingDiffers()
    {
        var extension = GetPathToExtensionThatCannotBeLoaded();

        var messages = CaptureSessionMessages(() =>
        {
            // On Windows those two paths are the same file, and a second warning about it tells the user
            // nothing they cannot already see in the first.
            TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(new List<string> { extension });
            TestPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(new List<string> { extension.ToUpperInvariant() });
        });

        Assert.ContainsSingle(messages);
    }

    [TestMethod]
    public void GetTestExtensionsFromAssemblyShouldNotWarnWhenOnlySomeTypesFailToLoad()
    {
        var filePath = GetPathToExtensionThatCannotBeLoaded();
        var assembly = new PartiallyLoadedAssembly(typeof(ValidDiscoverer), null);
        var pluginInfos = new Dictionary<string, TestDiscovererPluginInformation>();

        var messages = CaptureSessionMessages(
            () => TestPluginDiscoverer.GetTestExtensionsFromAssembly<TestDiscovererPluginInformation, ITestDiscoverer>(assembly, pluginInfos, filePath, reportFailures: true));

        // Adapters that reference an older ObjectModel throw this on a run that is otherwise completely fine,
        // see https://github.com/microsoft/vstest/issues/290. Warning here would put a warning on green runs.
        Assert.IsEmpty(messages);

        // And the types that did load are still discovered.
        var expected = new TestDiscovererPluginInformation(typeof(ValidDiscoverer));
        Assert.IsTrue(pluginInfos.ContainsKey(expected.IdentifierData!));
    }

    [TestMethod]
    public void GetTestExtensionsFromAssemblyShouldWarnWithTheReasonWhenNoTypeLoads()
    {
        var filePath = GetPathToExtensionThatCannotBeLoaded();
        var assembly = new PartiallyLoadedAssembly();
        var pluginInfos = new Dictionary<string, TestDiscovererPluginInformation>();

        var messages = CaptureSessionMessages(
            () => TestPluginDiscoverer.GetTestExtensionsFromAssembly<TestDiscovererPluginInformation, ITestDiscoverer>(assembly, pluginInfos, filePath, reportFailures: true));

        var message = messages.Single();
        Assert.AreEqual(TestMessageLevel.Warning, message.Level);
        Assert.Contains(filePath, message.Message);

        // The whole point of the message is to say which dependency is missing, instead of telling the user to
        // re-run with /diag.
        Assert.Contains(MissingDependencyName, message.Message);
    }

    /// <summary>
    /// A file that is guaranteed to not be loadable, and that no other test has reported yet.
    /// </summary>
    private static string GetPathToExtensionThatCannotBeLoaded()
        => $"ThisExtensionCannotBeLoaded{Guid.NewGuid():N}.dll";

    private static List<TestRunMessageEventArgs> CaptureSessionMessages(Action action)
    {
        var messages = new List<TestRunMessageEventArgs>();
        void OnTestRunMessage(object? sender, TestRunMessageEventArgs args) => messages.Add(args);

        TestSessionMessageLogger.Instance.TestRunMessage += OnTestRunMessage;
        try
        {
            action();
        }
        finally
        {
            TestSessionMessageLogger.Instance.TestRunMessage -= OnTestRunMessage;
        }

        return messages;
    }

    #region Implementations

    private const string MissingDependencyName = "Microsoft.Bcl.AsyncInterfaces";

    /// <summary>
    /// An assembly that loaded but whose types did not, the way a real adapter behaves when one of its
    /// dependencies is missing. <see cref="ReflectionTypeLoadException.Types"/> holds null for every type that
    /// failed, so passing no type at all stands for an assembly nothing could be loaded from.
    /// </summary>
    private sealed class PartiallyLoadedAssembly : Assembly
    {
        private readonly Type?[] _loadedTypes;

        public PartiallyLoadedAssembly(params Type?[] loadedTypes) => _loadedTypes = loadedTypes;

        public override string FullName => "PartiallyLoadedAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public override Type[] GetTypes()
            => throw new ReflectionTypeLoadException(
                _loadedTypes,
                new Exception[] { new FileNotFoundException($"Could not load file or assembly '{MissingDependencyName}, Version=9.0.0.8, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. The system cannot find the file specified.") });

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
