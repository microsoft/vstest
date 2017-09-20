// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    /// <summary>
    /// Options to be passed into the Test Platform during Discovery/Execution.
    /// </summary>
    public class TestPlatformOptions
    {
        /// <summary>
        /// Gets or sets the filter criteria for test cases.
        /// </summary>
        /// <remarks>
        /// This is only used when running tests with sources.
        /// </remarks>
        public string TestCaseFilter { get; set; }
    }
}
