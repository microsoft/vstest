using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.TestPlatform.TestUtilities
{
    #region Testable implementation

    public class TestableTestPluginCache : TestPluginCache
    {
        public Action Action;
        public TestableTestPluginCache(List<string> extensionsPath)
        {
            TestDiscoveryExtensionManager.Destroy();
            TestExecutorExtensionManager.Destroy();
            SettingsProviderExtensionManager.Destroy();
            this.UpdateExtensions(extensionsPath, skipExtensionFilters: false);
        }

        public TestableTestPluginCache() : this(new List<string>())
        {
        }

        protected override IEnumerable<string> GetFilteredExtensions(List<string> extensions, string searchPattern)
        {
            this.Action?.Invoke();
            return extensions;
        }

        new public void SetupAssemblyResolver(string extensionAssembly)
        {
            base.SetupAssemblyResolver(extensionAssembly);
        }
    }

    #endregion

    public static class TestPluginCacheHelper
    {
        public static TestableTestPluginCache SetupMockAdditionalPathExtensions(Type callingTest)
        {
            return SetupMockAdditionalPathExtensions(
                new string[] { callingTest.GetTypeInfo().Assembly.Location });
        }

        public static TestableTestPluginCache SetupMockAdditionalPathExtensions(string[] extensions)
        {
            var mockFileHelper = new Mock<IFileHelper>();
            var testPluginCache = new TestableTestPluginCache();

            TestPluginCache.Instance = testPluginCache;

            // Stub the default extensions folder.
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);

            TestPluginCache.Instance.UpdateExtensions(extensions, true);

            return testPluginCache;
        }

        public static void SetupMockExtensions(Type callingTest, Mock<IFileHelper> mockFileHelper = null)
        {
            SetupMockExtensions(callingTest, () => { }, mockFileHelper);
        }

        public static void SetupMockExtensions(Type callingTest, Action callback, Mock<IFileHelper> mockFileHelper = null)
        {
            SetupMockExtensions(new[] { callingTest.GetTypeInfo().Assembly.Location }, callback, mockFileHelper);
        }

        public static void SetupMockExtensions(string[] extensions, Action callback, Mock<IFileHelper> mockFileHelper = null)
        {
            // Setup mocks.
            if (mockFileHelper == null)
            {
                mockFileHelper = new Mock<IFileHelper>();
            }

            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);

            var testableTestPluginCache = new TestableTestPluginCache(extensions.ToList());
            testableTestPluginCache.Action = callback;

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;
        }

        public static void ResetExtensionsCache()
        {
            TestPluginCache.Instance = null;
            SettingsProviderExtensionManager.Destroy();
        }
    }
}
