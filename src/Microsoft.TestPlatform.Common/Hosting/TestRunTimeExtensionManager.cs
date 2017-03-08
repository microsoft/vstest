// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Hosting
{
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System.Collections.Generic;

    /// <summary>
    /// Manages loading and provides access to testhost extensions implementing the
    /// ITestRunTimeProvider interface.
    /// </summary>
    internal class TestRunTimeExtensionManager : TestExtensionManager<ITestRunTimeProvider, ITestRunTimeCapabilities>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="unfilteredTestExtensions">
        /// The unfiltered Test Extensions.
        /// </param>
        /// <param name="testExtensions">
        /// The test Extensions.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <remarks>
        /// The constructor is not public because the factory method should be used to get instances of this class.
        /// </remarks>
        protected TestRunTimeExtensionManager(
            IEnumerable<LazyExtension<ITestRunTimeProvider, Dictionary<string, object>>> unfilteredTestExtensions,
            IEnumerable<LazyExtension<ITestRunTimeProvider, ITestRunTimeCapabilities>> testExtensions,
            IMessageLogger logger)
            : base(unfilteredTestExtensions, testExtensions, logger)
        {
        }

        /// <summary>
        /// Gets an instance of the TestLoggerExtensionManager.
        /// </summary>
        /// <param name="messageLogger">
        /// The message Logger.
        /// </param>
        /// <returns>
        /// The TestLoggerExtensionManager.
        /// </returns>
        public static TestRunTimeExtensionManager Create(IMessageLogger messageLogger)
        {
            IEnumerable<LazyExtension<ITestRunTimeProvider, ITestRunTimeCapabilities>> filteredTestExtensions;
            IEnumerable<LazyExtension<ITestRunTimeProvider, Dictionary<string, object>>> unfilteredTestExtensions;

            TestPluginManager.Instance.GetTestExtensions<ITestRunTimeProvider, ITestRunTimeCapabilities, TestRunTimeMetadata>(
                out unfilteredTestExtensions,
                out filteredTestExtensions);

            return new TestRunTimeExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
        }
    }

}
