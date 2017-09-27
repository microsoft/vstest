// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

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

        /// <summary> 
        /// Gets or sets the filter options if there are any. 
        /// </summary> 
        /// <remarks> 
        /// This is will be valid only if TestCase filter is present. 
        /// </remarks> 
        public FilterOptions FilterOptions { get; set; }
    }
}
