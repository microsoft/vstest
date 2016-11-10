// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System.Collections.Generic;

    /// <summary>
    /// Manages loading and provides access to logging extensions implementing the
    /// ITestLogger interface.
    /// </summary>
    internal class TestLoggerExtensionManager : TestExtensionManager<ITestLogger, ITestLoggerCapabilities>
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
        protected TestLoggerExtensionManager(
            IEnumerable<LazyExtension<ITestLogger, Dictionary<string, object>>> unfilteredTestExtensions,
            IEnumerable<LazyExtension<ITestLogger, ITestLoggerCapabilities>> testExtensions,
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
        public static TestLoggerExtensionManager Create(IMessageLogger messageLogger)
        {
            IEnumerable<LazyExtension<ITestLogger, ITestLoggerCapabilities>> filteredTestExtensions;
            IEnumerable<LazyExtension<ITestLogger, Dictionary<string, object>>> unfilteredTestExtensions;

            TestPluginManager.Instance.GetTestExtensions<ITestLogger, ITestLoggerCapabilities, TestLoggerMetadata>(
                out unfilteredTestExtensions,
                out filteredTestExtensions);

            return new TestLoggerExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
        }
    }

}
