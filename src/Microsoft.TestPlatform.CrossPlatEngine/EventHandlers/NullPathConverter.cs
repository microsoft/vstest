// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.Collections.Generic;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.ObjectModel;

internal class NullPathConverter : IPathConverter
{
    Collection<AttachmentSet> IPathConverter.UpdateAttachmentSets(Collection<AttachmentSet> attachmentSets, PathConversionDirection _) => attachmentSets;

    ICollection<AttachmentSet> IPathConverter.UpdateAttachmentSets(ICollection<AttachmentSet> attachmentSets, PathConversionDirection _) => attachmentSets;

    DiscoveryCriteria IPathConverter.UpdateDiscoveryCriteria(DiscoveryCriteria discoveryCriteria, PathConversionDirection _) => discoveryCriteria;

    string IPathConverter.UpdatePath(string path, PathConversionDirection _) => path;

    IEnumerable<string> IPathConverter.UpdatePaths(IEnumerable<string> enumerable, PathConversionDirection _) => enumerable;

    TestCase IPathConverter.UpdateTestCase(TestCase testCase, PathConversionDirection _) => testCase;

    IEnumerable<TestCase> IPathConverter.UpdateTestCases(IEnumerable<TestCase> testCases, PathConversionDirection _) => testCases;

    TestRunChangedEventArgs IPathConverter.UpdateTestRunChangedEventArgs(TestRunChangedEventArgs testRunChangedArgs, PathConversionDirection _) => testRunChangedArgs;

    TestRunCompleteEventArgs IPathConverter.UpdateTestRunCompleteEventArgs(TestRunCompleteEventArgs testRunCompleteEventArgs, PathConversionDirection _) => testRunCompleteEventArgs;

    TestRunCriteriaWithSources IPathConverter.UpdateTestRunCriteriaWithSources(TestRunCriteriaWithSources testRunCriteriaWithSources, PathConversionDirection _) => testRunCriteriaWithSources;

    TestRunCriteriaWithTests IPathConverter.UpdateTestRunCriteriaWithTests(TestRunCriteriaWithTests testRunCriteriaWithTests, PathConversionDirection _) => testRunCriteriaWithTests;
}
