// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;

    /// <summary>
    /// The test executor plugin information.
    /// </summary>
    internal class TestExecutorPluginInformation : TestExtensionPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="testExecutorType"> The test Executor Type. </param>
        public TestExecutorPluginInformation(Type testExecutorType)
            : base(testExecutorType)
        {
        }
    }
}
