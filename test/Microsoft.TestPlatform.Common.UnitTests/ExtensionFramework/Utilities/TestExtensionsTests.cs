// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework.Utilities;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

[TestClass]
public class TestExtensionsTests
{
    private TestExtensions _testExtensions;

    [TestInitialize]
    public void TestInit()
    {
        _testExtensions = new TestExtensions();
    }

    [TestMethod]
    public void AddExtensionsShouldNotThrowIfExtensionsIsNull()
    {
        _testExtensions.AddExtension<TestPluginInformation>(null);

        // Validate that the default state does not change.
        Assert.IsNull(_testExtensions.TestDiscoverers);
    }

    [TestMethod]
    public void AddExtensionsShouldNotThrowIfExistingExtensionCollectionIsNull()
    {
        var testDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            {
                "td",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests))
            }
        };

        _testExtensions.AddExtension(testDiscoverers);

        Assert.IsNotNull(_testExtensions.TestDiscoverers);
        CollectionAssert.AreEqual(_testExtensions.TestDiscoverers, testDiscoverers);

        // Validate that the others remain same.
        Assert.IsNull(_testExtensions.TestExecutors);
        Assert.IsNull(_testExtensions.TestSettingsProviders);
        Assert.IsNull(_testExtensions.TestLoggers);
    }

    [TestMethod]
    public void AddExtensionsShouldAddToExistingExtensionCollection()
    {
        var testDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td1", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) },
            { "td2", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        _testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        // Act.
        _testExtensions.AddExtension(testDiscoverers);

        // Validate.
        var expectedTestExtensions = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) },
            { "td1", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) },
            { "td2", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        CollectionAssert.AreEqual(_testExtensions.TestDiscoverers.Keys, expectedTestExtensions.Keys);

        // Validate that the others remain same.
        Assert.IsNull(_testExtensions.TestExecutors);
        Assert.IsNull(_testExtensions.TestSettingsProviders);
        Assert.IsNull(_testExtensions.TestLoggers);
    }

    [TestMethod]
    public void AddExtensionsShouldNotAddAnAlreadyExistingExtensionToTheCollection()
    {
        var testDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        _testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        // Act.
        _testExtensions.AddExtension(testDiscoverers);

        // Validate.
        CollectionAssert.AreEqual(_testExtensions.TestDiscoverers.Keys, testDiscoverers.Keys);

        // Validate that the others remain same.
        Assert.IsNull(_testExtensions.TestExecutors);
        Assert.IsNull(_testExtensions.TestSettingsProviders);
        Assert.IsNull(_testExtensions.TestLoggers);
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldReturnNullIfNoExtensionsPresent()
    {
        var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

        Assert.IsNull(_testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation));
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldNotThrowIfExtensionAssemblyIsNull()
    {
        _testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        Assert.IsNull(_testExtensions.GetExtensionsDiscoveredFromAssembly(null));
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestDiscoverers()
    {
        var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

        _testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) },
            { "td1", new TestDiscovererPluginInformation(typeof(TestExtensions)) }
        };

        var extensions = _testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

        var expectedExtensions = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };
        CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestDiscoverers.Keys);
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestExecutors()
    {
        var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

        _testExtensions.TestExecutors = new Dictionary<string, TestExecutorPluginInformation>
        {
            { "te", new TestExecutorPluginInformation(typeof(TestExtensionsTests)) },
            { "te1", new TestExecutorPluginInformation(typeof(TestExtensions)) }
        };

        var extensions = _testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

        var expectedExtensions = new Dictionary<string, TestExecutorPluginInformation>
        {
            { "te", new TestExecutorPluginInformation(typeof(TestExtensionsTests)) }
        };
        CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestExecutors.Keys);
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestSettingsProviders()
    {
        var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

        _testExtensions.TestSettingsProviders = new Dictionary<string, TestSettingsProviderPluginInformation>
        {
            { "tsp", new TestSettingsProviderPluginInformation(typeof(TestExtensionsTests)) },
            { "tsp1", new TestSettingsProviderPluginInformation(typeof(TestExtensions)) }
        };

        var extensions = _testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

        var expectedExtensions = new Dictionary<string, TestSettingsProviderPluginInformation>
        {
            { "tsp", new TestSettingsProviderPluginInformation(typeof(TestExtensionsTests)) }
        };
        CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestSettingsProviders.Keys);
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestLoggers()
    {
        var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

        _testExtensions.TestLoggers = new Dictionary<string, TestLoggerPluginInformation>
        {
            { "tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)) },
            { "tl1", new TestLoggerPluginInformation(typeof(TestExtensions)) }
        };

        var extensions = _testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

        var expectedExtensions = new Dictionary<string, TestLoggerPluginInformation>
        {
            { "tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)) }
        };
        CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestLoggers.Keys);
    }

    [TestMethod]
    public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestDiscoveresAndLoggers()
    {
        var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

        _testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };

        _testExtensions.TestLoggers = new Dictionary<string, TestLoggerPluginInformation>
        {
            { "tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)) }
        };

        var extensions = _testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

        var expectedDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>
        {
            { "td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)) }
        };
        CollectionAssert.AreEqual(expectedDiscoverers.Keys, extensions.TestDiscoverers.Keys);

        var expectedLoggers = new Dictionary<string, TestLoggerPluginInformation>
        {
            { "tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)) }
        };
        CollectionAssert.AreEqual(expectedLoggers.Keys, extensions.TestLoggers.Keys);
    }
}
