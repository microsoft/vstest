// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Constants = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Constants;
    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

    [TestClass]
    public class InProcDataCollectionExtensionManagerTests
    {
        private readonly string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";
        private Mock<ITestEventsPublisher> mockTestEventsPublisher;
        private TestableInProcDataCollectionExtensionManager inProcDataCollectionManager;
        private static readonly string temp = Path.GetTempPath();
        private readonly string defaultCodebase = Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug");
        private Mock<IFileHelper> mockFileHelper;
        private TestPluginCache testPluginCache;

        [TestInitialize]
        public void TestInit()
        {
            mockTestEventsPublisher = new Mock<ITestEventsPublisher>();
            mockFileHelper = new Mock<IFileHelper>();
            testPluginCache = TestPluginCache.Instance;
            inProcDataCollectionManager = new TestableInProcDataCollectionExtensionManager(settingsXml, mockTestEventsPublisher.Object, defaultCodebase, testPluginCache, mockFileHelper.Object);
        }

        [TestMethod]
        public void InProcDataCollectionExtensionManagerShouldLoadsDataCollectorsFromRunSettings()
        {
            var dataCollector = inProcDataCollectionManager.InProcDataCollectors.First().Value as MockDataCollector;

            Assert.IsTrue(inProcDataCollectionManager.IsInProcDataCollectionEnabled, "InProcDataCollection must be enabled if runsettings contains inproc datacollectors.");
            Assert.AreEqual(1, inProcDataCollectionManager.InProcDataCollectors.Count, "One Datacollector must be registered");

            Equals(dataCollector.AssemblyQualifiedName, "TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a");
            Equals(dataCollector.CodeBase, Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests.dll"));
            Equals(dataCollector.Configuration.OuterXml, @"<Configuration><Port>4312</Port></Configuration>");
        }

        [TestMethod]
        public void InProcDataCollectionExtensionManagerLoadsDataCollectorFromDefaultCodebaseIfExistsAndCodebaseIsRelative()
        {
            string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

            mockFileHelper.Setup(fh => fh.Exists(Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests.dll"))).Returns(true);
            inProcDataCollectionManager = new TestableInProcDataCollectionExtensionManager(settingsXml, mockTestEventsPublisher.Object, defaultCodebase, testPluginCache, mockFileHelper.Object);

            var codebase = (inProcDataCollectionManager.InProcDataCollectors.Values.First() as MockDataCollector).CodeBase;
            Assert.AreEqual(Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests.dll"), codebase);
        }

        [TestMethod]
        public void InProcDataCollectionExtensionManagerLoadsDataCollectorFromTestPluginCacheIfExistsAndCodebaseIsRelative()
        {
            string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='TestImpactListenerDataCollector.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

            testPluginCache.UpdateExtensions(new List<string> { Path.Combine(temp, "source", ".nuget", "TestImpactListenerDataCollector.dll") }, true);
            mockFileHelper.Setup(fh => fh.Exists(Path.Combine(temp, "source", ".nuget", "TestImpactListenerDataCollector.dll"))).Returns(true);

            inProcDataCollectionManager = new TestableInProcDataCollectionExtensionManager(settingsXml, mockTestEventsPublisher.Object, defaultCodebase, testPluginCache, mockFileHelper.Object);

            var codebase = (inProcDataCollectionManager.InProcDataCollectors.Values.First() as MockDataCollector).CodeBase;
            Assert.AreEqual(Path.Combine(temp,"source", ".nuget", "TestImpactListenerDataCollector.dll"), codebase);
        }

        [TestMethod]
        public void InProcDataCollectionExtensionManagerLoadsDataCollectorFromGivenCodebaseIfCodebaseIsAbsolute()
        {
            string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='\\DummyPath\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";
            inProcDataCollectionManager = new TestableInProcDataCollectionExtensionManager(settingsXml, mockTestEventsPublisher.Object, defaultCodebase, testPluginCache, mockFileHelper.Object);

            var codebase = (inProcDataCollectionManager.InProcDataCollectors.Values.First() as MockDataCollector).CodeBase;
            Assert.AreEqual("\\\\DummyPath\\TestImpactListener.Tests.dll", codebase);
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingMultipleDataCollector()
        {
            var temp = Path.GetTempPath();
            var path1 = Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests1.dll");
            var path2 = Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests2.dll");
            var multiSettingsXml = $@"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='{path1}'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                            <InProcDataCollector friendlyName='InProcDataCol' uri='InProcDataCollector://Microsoft/InProcDataCol/2.0' assemblyQualifiedName='TestImpactListener.Tests, Version=2.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='{path2}'>
                                                <Configuration>
                                                    <Port>4313</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

            inProcDataCollectionManager = new TestableInProcDataCollectionExtensionManager(multiSettingsXml, mockTestEventsPublisher.Object, defaultCodebase, testPluginCache, mockFileHelper.Object);
            bool secondOne = false;
            MockDataCollector dataCollector1 = null;
            MockDataCollector dataCollector2 = null;

            foreach (var inProcDC in inProcDataCollectionManager.InProcDataCollectors.Values)
            {
                if (secondOne)
                {
                    dataCollector2 = inProcDC as MockDataCollector;
                }
                else
                {
                    dataCollector1 = inProcDC as MockDataCollector;
                    secondOne = true;
                }
            }

            Assert.IsTrue(string.Equals(dataCollector1.AssemblyQualifiedName, "TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(dataCollector1.CodeBase, Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests1.dll"), StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(dataCollector1.Configuration.OuterXml, @"<Configuration><Port>4312</Port></Configuration>", StringComparison.OrdinalIgnoreCase));

            Assert.IsTrue(string.Equals(dataCollector2.AssemblyQualifiedName, "TestImpactListener.Tests, Version=2.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(dataCollector2.CodeBase, Path.Combine(temp, "repos", "MSTest", "src", "managed", "TestPlatform", "TestImpactListener.Tests", "bin", "Debug", "TestImpactListener.Tests2.dll"), StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(dataCollector2.Configuration.OuterXml, @"<Configuration><Port>4313</Port></Configuration>", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InProcDataCollectionExtensionManagerWillNotEnableDataCollectionForInavlidSettingsXml()
        {
            var invalidSettingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll' value='Invalid'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

            var manager = new InProcDataCollectionExtensionManager(invalidSettingsXml, mockTestEventsPublisher.Object, defaultCodebase, testPluginCache);
            Assert.IsFalse(manager.IsInProcDataCollectionEnabled, "InProcDataCollection must be disabled on invalid settings.");
        }
        [TestMethod]
        public void TriggerSessionStartShouldBeCalledWithCorrectTestSources()
        {
            var properties = new Dictionary<string, object>
            {
                { "TestSources", new List<string>() { "testsource1.dll", "testsource2.dll" } }
            };

            var mockDataCollector = inProcDataCollectionManager.InProcDataCollectors.Values.FirstOrDefault() as MockDataCollector;

            mockTestEventsPublisher.Raise(x => x.SessionStart += null, new SessionStartEventArgs(properties));
            Assert.IsTrue((mockDataCollector.TestSessionStartCalled == 1), "TestSessionStart must be called on datacollector");

            Assert.IsTrue(mockDataCollector.TestSources.Contains("testsource1.dll"));
            Assert.IsTrue(mockDataCollector.TestSources.Contains("testsource2.dll"));
        }


        [TestMethod]
        public void TriggerSessionStartShouldCallInProcDataCollector()
        {
            mockTestEventsPublisher.Raise(x => x.SessionStart += null, new SessionStartEventArgs());

            var mockDataCollector = inProcDataCollectionManager.InProcDataCollectors.Values.FirstOrDefault() as MockDataCollector;
            Assert.IsTrue((mockDataCollector.TestSessionStartCalled == 1), "TestSessionStart must be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestSessionEndCalled == 0), "TestSessionEnd must NOT be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestCaseStartCalled == 0), "TestCaseStart must NOT be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestCaseEndCalled == 0), "TestCaseEnd must NOT be called on datacollector");
        }

        [TestMethod]
        public void TriggerSessionEndShouldCallInProcDataCollector()
        {
            mockTestEventsPublisher.Raise(x => x.SessionEnd += null, new SessionEndEventArgs());

            var mockDataCollector = inProcDataCollectionManager.InProcDataCollectors.Values.FirstOrDefault() as MockDataCollector;
            Assert.IsTrue((mockDataCollector.TestSessionStartCalled == 0), "TestSessionEnd must NOT be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestSessionEndCalled == 1), "TestSessionStart must be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestCaseStartCalled == 0), "TestCaseStart must NOT be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestCaseEndCalled == 0), "TestCaseEnd must NOT be called on datacollector");
        }

        [TestMethod]
        public void TriggerTestCaseStartShouldCallInProcDataCollector()
        {
            var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
            // random guid
            testCase.Id = new Guid("3871B3B0-2853-406B-BB61-1FE1764116FD");
            mockTestEventsPublisher.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testCase));

            var mockDataCollector = inProcDataCollectionManager.InProcDataCollectors.Values.FirstOrDefault() as MockDataCollector;

            Assert.IsTrue((mockDataCollector.TestCaseStartCalled == 1), "TestCaseStart must be called on datacollector");
            Assert.IsTrue((mockDataCollector.TestCaseEndCalled == 0), "TestCaseEnd must NOT be called on datacollector");
        }

        [TestMethod]
        public void TriggerTestCaseEndShouldtBeCalledMultipleTimesInDataDrivenScenario()
        {
            var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
            // random guid
            testCase.Id = new Guid("3871B3B0-2853-406B-BB61-1FE1764116FD");
            mockTestEventsPublisher.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testCase));
            mockTestEventsPublisher.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testCase, TestOutcome.Passed));
            mockTestEventsPublisher.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testCase));
            mockTestEventsPublisher.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testCase, TestOutcome.Failed));

            var mockDataCollector = inProcDataCollectionManager.InProcDataCollectors.Values.FirstOrDefault() as MockDataCollector;
            Assert.IsTrue((mockDataCollector.TestCaseStartCalled == 2), "TestCaseStart must only be called once");
            Assert.IsTrue((mockDataCollector.TestCaseEndCalled == 2), "TestCaseEnd must only be called once");
        }

        internal class TestableInProcDataCollectionExtensionManager : InProcDataCollectionExtensionManager
        {
            public TestableInProcDataCollectionExtensionManager(string runSettings, ITestEventsPublisher mockTestEventsPublisher, string defaultCodebase, TestPluginCache testPluginCache, IFileHelper fileHelper) 
                : base(runSettings, mockTestEventsPublisher, defaultCodebase, testPluginCache, fileHelper)
            {
            }

            protected override IInProcDataCollector CreateDataCollector(string assemblyQualifiedName, string codebase, XmlElement configuration, TypeInfo interfaceTypeInfo)
            {
                return new MockDataCollector(assemblyQualifiedName, codebase,configuration);
            }
        }

        public class MockDataCollector : IInProcDataCollector
        {
            public MockDataCollector(string assemblyQualifiedName, string codebase, XmlElement configuration)
            {
                AssemblyQualifiedName = assemblyQualifiedName;
                CodeBase = codebase;
                Configuration = configuration;
            }

            public string AssemblyQualifiedName
            {
                get;
                private set;
            }

            public string CodeBase
            {
                get;
                private set;
            }

            public XmlElement Configuration
            {
                get;
                private set;
            }

            public int TestSessionStartCalled { get; private set; }
            public int TestSessionEndCalled { get; private set; }
            public int TestCaseStartCalled { get; private set; }
            public int TestCaseEndCalled { get; private set; }
            public IEnumerable<string> TestSources
            {
                get;
                private set;
            }

            public void LoadDataCollector(IDataCollectionSink inProcDataCollectionSink)
            {
                // Do Nothing
            }

            public void TriggerInProcDataCollectionMethod(string methodName, InProcDataCollectionArgs methodArg)
            {
                switch (methodName)
                {
                    case Constants.TestSessionStartMethodName: TestSessionStartMethodCalled(methodArg as TestSessionStartArgs); break;
                    case Constants.TestSessionEndMethodName: TestSessionEndCalled++; break;
                    case Constants.TestCaseStartMethodName: TestCaseStartCalled++; break;
                    case Constants.TestCaseEndMethodName: TestCaseEndCalled++; break;
                    default: break;
                }
            }

            private void TestSessionStartMethodCalled(TestSessionStartArgs testSessionStartArgs)
            {
                TestSessionStartCalled++;
                TestSources = testSessionStartArgs.GetPropertyValue<IEnumerable<string>>("TestSources");
            }
        }
    }
}
