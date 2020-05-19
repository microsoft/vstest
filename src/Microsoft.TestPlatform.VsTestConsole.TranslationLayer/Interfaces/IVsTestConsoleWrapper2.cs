// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Controller for various test operations on the test runner.
    /// </summary>
    public interface IVsTestConsoleWrapper2 : IVsTestConsoleWrapper
    {
        /// <summary>
        /// Provides back all attachements to TestPlatform for additional processing (for example merging)
        /// </summary>
        /// <param name="attachments">List of attachements</param>
        /// <param name="testSessionEventsHandler">EventHandler to receive session complete event</param>
        void FinalizeMultiTestRuns(IEnumerable<AttachmentSet> attachments, ITestSessionEventsHandler testSessionEventsHandler);
       // void FinalizeTests(IEnumerable<AttachmentSet> attachments, ITestSessionEventsHandler testSessionEventsHandler);
    }
}
