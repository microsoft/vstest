// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// The factory class that provides an instance of the test platform.
    /// </summary>
    public class TestPlatformFactory
    {
        private static ITestPlatform testPlatform;

        /// <summary>
        /// Gets an instance of the test platform.
        /// </summary>
        /// <returns> The <see cref="ITestPlatform"/> instance. </returns>
        public static ITestPlatform GetTestPlatform()
        {
            return testPlatform ?? (testPlatform = new TestPlatform());
        }
    }
}
