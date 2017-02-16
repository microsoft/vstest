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
    /// ITestHostProvider interface.
    /// </summary>
    internal class TestHostExtensionManager : TestExtensionManager<ITestHostProvider, ITestHostCapabilities>
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
        protected TestHostExtensionManager(
            IEnumerable<LazyExtension<ITestHostProvider, Dictionary<string, object>>> unfilteredTestExtensions,
            IEnumerable<LazyExtension<ITestHostProvider, ITestHostCapabilities>> testExtensions,
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
        public static TestHostExtensionManager Create(IMessageLogger messageLogger)
        {
            IEnumerable<LazyExtension<ITestHostProvider, ITestHostCapabilities>> filteredTestExtensions;
            IEnumerable<LazyExtension<ITestHostProvider, Dictionary<string, object>>> unfilteredTestExtensions;

            TestPluginManager.Instance.GetTestExtensions<ITestHostProvider, ITestHostCapabilities, TestHostMetadata>(
                out unfilteredTestExtensions,
                out filteredTestExtensions);

            return new TestHostExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
        }
    }

}
