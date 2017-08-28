// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    using Newtonsoft.Json;

    /// <summary>
    /// The test run criteria with sources.
    /// </summary>
    public class TestRunCriteriaWithSources
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteriaWithSources"/> class.
        /// </summary>
        /// <param name="adapterSourceMap"> The adapter source map.  </param>
        /// <param name="packages"> The packages which actually contain sources(UWP).</param>
        /// <param name="runSettings"> The run settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        [JsonConstructor]
        public TestRunCriteriaWithSources(Dictionary<string, IEnumerable<string>> adapterSourceMap, IEnumerable<string> packages, string runSettings, TestExecutionContext testExecutionContext)
        {
            this.AdapterSourceMap = adapterSourceMap;
            this.Packages = packages;
            this.RunSettings = runSettings;
            this.TestExecutionContext = testExecutionContext;
        }

        /// <summary>
        /// Gets the adapter source map.
        /// </summary>
        public Dictionary<string, IEnumerable<string>> AdapterSourceMap { get; private set; }

        /// <summary>
        /// Gets the run settings.
        /// </summary>
        public string RunSettings { get; private set; }

        /// <summary>
        /// Gets or sets the test execution context.
        /// </summary>
        public TestExecutionContext TestExecutionContext { get; set; }

        /// <summary>
        /// Gets the test Containers (e.g. DLL/EXE/artifacts to scan)
        /// </summary>
        public IEnumerable<string> Packages { get; private set; }
    }
}