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
    Collection<AttachmentSet> IPathConverter.UpdateAttachmentSets(Collection<AttachmentSet> attachmentSets, PathConversionDirection updateDirection)
    {
        return attachmentSets;
    }

    ICollection<AttachmentSet> IPathConverter.UpdateAttachmentSets(ICollection<AttachmentSet> attachmentSets, PathConversionDirection updateDirection)
    {
        return attachmentSets;
    }

    DiscoveryCriteria IPathConverter.UpdateDiscoveryCriteria(DiscoveryCriteria discoveryCriteria, PathConversionDirection updateDirection)
    {
        return discoveryCriteria;
    }

    string IPathConverter.UpdatePath(string path, PathConversionDirection updateDirection)
    {
        return path;
    }

    IEnumerable<string> IPathConverter.UpdatePaths(IEnumerable<string> enumerable, PathConversionDirection updateDirection)
    {
        return enumerable;
    }

    TestCase IPathConverter.UpdateTestCase(TestCase testCase, PathConversionDirection updateDirection)
    {
        return testCase;
    }

    IEnumerable<TestCase> IPathConverter.UpdateTestCases(IEnumerable<TestCase> testCases, PathConversionDirection updateDirection)
    {
        return testCases;
    }

    TestRunChangedEventArgs IPathConverter.UpdateTestRunChangedEventArgs(TestRunChangedEventArgs testRunChangedArgs, PathConversionDirection updateDirection)
    {
        return testRunChangedArgs;
    }

    TestRunCompleteEventArgs IPathConverter.UpdateTestRunCompleteEventArgs(TestRunCompleteEventArgs testRunCompleteEventArgs, PathConversionDirection updateDirection)
    {
        return testRunCompleteEventArgs;
    }

    TestRunCriteriaWithSources IPathConverter.UpdateTestRunCriteriaWithSources(TestRunCriteriaWithSources testRunCriteriaWithSources, PathConversionDirection updateDirection)
    {
        return testRunCriteriaWithSources;
    }

    TestRunCriteriaWithTests IPathConverter.UpdateTestRunCriteriaWithTests(TestRunCriteriaWithTests testRunCriteriaWithTests, PathConversionDirection updateDirection)
    {
        return testRunCriteriaWithTests;
    }
}
