// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Defines the Data Collection Manager for Data Collectors.
    /// </summary>
    internal interface IDataCollectionManager : IDisposable
    {
        /// <summary>
        /// Loads and initializes data collector plugins.
        /// </summary>
        /// <param name="settingsXml">Run Settings which has DataCollector configuration.</param>
        /// <returns>Environment variables.</returns>
        Dictionary<string, string> LoadDataCollectors(RunSettings settingsXml);

        /// <summary>
        /// Raises TestCaseStart event to all data collectors configured for run.
        /// </summary>
        /// <param name="testCaseStartEventArgs">TestCaseStart event.</param>
        void TestCaseStarted(TestCaseStartEventArgs testCaseStartEventArgs);

        /// <summary>
        /// Raises TestCaseEnd event to all data collectors configured for run.
        /// </summary>
        /// <param name="testCase">Test case which is complete.</param>
        /// <param name="testOutcome">Outcome of the test case.</param>
        /// <returns>Collection of  testCase attachmentSet.</returns>
        Collection<AttachmentSet> TestCaseEnded(TestCase testCase, TestOutcome testOutcome);

        /// <summary>
        /// Raises SessionStart event to all data collectors configured for run.
        /// </summary>
        /// <returns>Are test case level events required.</returns>
        bool SessionStarted();

        /// <summary>
        /// Raises SessionEnd event to all data collectors configured for run.
        /// </summary>
        /// <param name="isCancelled">Specified whether the run is cancelled or not.</param>
        /// <returns>Collection of session attachmentSet.</returns>
        Collection<AttachmentSet> SessionEnded(bool isCancelled);
    }
}