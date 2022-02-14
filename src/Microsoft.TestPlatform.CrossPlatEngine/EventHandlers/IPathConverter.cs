// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.Collections.Generic;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.ObjectModel;

internal interface IPathConverter
{
    internal string UpdatePath(string path, PathConversionDirection updateDirection);


    internal IEnumerable<string> UpdatePaths(IEnumerable<string> enumerable, PathConversionDirection updateDirection);

    internal TestCase UpdateTestCase(TestCase testCase, PathConversionDirection updateDirection);

    internal IEnumerable<TestCase> UpdateTestCases(IEnumerable<TestCase> testCases, PathConversionDirection updateDirection);


    internal TestRunCompleteEventArgs UpdateTestRunCompleteEventArgs(TestRunCompleteEventArgs testRunCompleteEventArgs, PathConversionDirection updateDirection);


    internal TestRunChangedEventArgs UpdateTestRunChangedEventArgs(TestRunChangedEventArgs testRunChangedArgs, PathConversionDirection updateDirection);


    internal Collection<AttachmentSet> UpdateAttachmentSets(Collection<AttachmentSet> attachmentSets, PathConversionDirection updateDirection);

    internal ICollection<AttachmentSet> UpdateAttachmentSets(ICollection<AttachmentSet> attachmentSets, PathConversionDirection updateDirection);

    internal DiscoveryCriteria UpdateDiscoveryCriteria(DiscoveryCriteria discoveryCriteria, PathConversionDirection updateDirection);

    internal TestRunCriteriaWithSources UpdateTestRunCriteriaWithSources(TestRunCriteriaWithSources testRunCriteriaWithSources, PathConversionDirection updateDirection);

    internal TestRunCriteriaWithTests UpdateTestRunCriteriaWithTests(TestRunCriteriaWithTests testRunCriteriaWithTests, PathConversionDirection updateDirection);
}