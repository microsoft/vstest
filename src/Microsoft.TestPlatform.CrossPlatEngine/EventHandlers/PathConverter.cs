// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.Collections.Generic;
using System.Linq;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using System.IO;

internal class PathConverter : IPathConverter
{
    // The path on this computer to which we deployed the test dll and test runner
    private readonly string _deploymentPath = "";
    // The path on the remote system where test dll was originally placed, and from which we
    // copied it to this system. For vstest.console, which is on the other side of this, the names
    // are inverted, it sends us their local path, and thinks about our local path as remote.
    private readonly string _originalPath = "";

    public PathConverter(string originalPath, string deploymentPath, IFileHelper fileHelper)
    {
        _originalPath = fileHelper.GetFullPath(originalPath).TrimEnd('\\').TrimEnd('/') + Path.DirectorySeparatorChar;
        _deploymentPath = fileHelper.GetFullPath(deploymentPath).TrimEnd('\\').TrimEnd('/') + Path.DirectorySeparatorChar;
    }

    public string UpdatePath(string path, PathConversionDirection updateDirection)
    {
        string find;
        string replaceWith;
        if (updateDirection == PathConversionDirection.Receive)
        {
            // Request is incoming, the path that is local to the sender (for us that is "remote" path)
            // needs to be replaced with our path
            find = _originalPath;
            replaceWith = _deploymentPath;
        }
        else
        {
            find = _deploymentPath;
            replaceWith = _originalPath;
        }

        var result = path?.Replace(find, replaceWith);
        return result;
    }

    public IEnumerable<string> UpdatePaths(IEnumerable<string> enumerable, PathConversionDirection updateDirection)
    {
        var updatedPaths = enumerable.Select(i => UpdatePath(i, updateDirection)).ToList();
        return updatedPaths;
    }

    public TestCase UpdateTestCase(TestCase testCase, PathConversionDirection updateDirection)
    {
        testCase.CodeFilePath = UpdatePath(testCase.CodeFilePath, updateDirection);
        testCase.Source = UpdatePath(testCase.Source, updateDirection);
        return testCase;
    }

    public IEnumerable<TestCase> UpdateTestCases(IEnumerable<TestCase> testCases, PathConversionDirection updateDirection)
    {
        var updatedTestCases = testCases.Select(tc => UpdateTestCase(tc, updateDirection)).ToList();

        return updatedTestCases;
    }

    public TestRunCompleteEventArgs UpdateTestRunCompleteEventArgs(TestRunCompleteEventArgs testRunCompleteEventArgs, PathConversionDirection updateDirection)
    {
        UpdateAttachmentSets(testRunCompleteEventArgs.AttachmentSets, updateDirection);
        return testRunCompleteEventArgs;
    }

    public TestRunChangedEventArgs UpdateTestRunChangedEventArgs(TestRunChangedEventArgs testRunChangedArgs, PathConversionDirection updateDirection)
    {
        UpdateTestResults(testRunChangedArgs.NewTestResults, updateDirection);
        UpdateTestCases(testRunChangedArgs.ActiveTests, updateDirection);
        return testRunChangedArgs;
    }

    public Collection<AttachmentSet> UpdateAttachmentSets(Collection<AttachmentSet> attachmentSets, PathConversionDirection updateDirection)
    {
        attachmentSets.Select(i => UpdateAttachmentSet(i, updateDirection)).ToList();
        return attachmentSets;
    }

    public ICollection<AttachmentSet> UpdateAttachmentSets(ICollection<AttachmentSet> attachmentSets, PathConversionDirection updateDirection)
    {
        attachmentSets.Select(i => UpdateAttachmentSet(i, updateDirection)).ToList();
        return attachmentSets;
    }

    private AttachmentSet UpdateAttachmentSet(AttachmentSet attachmentSet, PathConversionDirection updateDirection)
    {
        attachmentSet.Attachments.Select(a => UpdateAttachment(a, updateDirection)).ToList();
        return attachmentSet;
    }

    private UriDataAttachment UpdateAttachment(UriDataAttachment attachment, PathConversionDirection updateDirection)
    {
        // todo: convert uri?
        return attachment;
    }

    private IEnumerable<TestResult> UpdateTestResults(IEnumerable<TestResult> testResults, PathConversionDirection updateDirection)
    {
        // The incoming collection is IEnumerable, use foreach to make sure we always do the changes,
        // as opposed to using .Select which will never run unless you ask for results (which totally
        // did not happen to me, of course).
        foreach (var tr in testResults)
        {
            tr.Attachments.Select(a => UpdateAttachmentSet(a, updateDirection));
            UpdateTestCase(tr.TestCase, updateDirection);
        }

        return testResults;
    }

    public DiscoveryCriteria UpdateDiscoveryCriteria(DiscoveryCriteria discoveryCriteria, PathConversionDirection updateDirection)
    {
        discoveryCriteria.Package = UpdatePath(discoveryCriteria.Package, updateDirection);
        foreach (var adapter in discoveryCriteria.AdapterSourceMap.ToList())
        {
            var updatedPaths = UpdatePaths(adapter.Value, updateDirection);
            discoveryCriteria.AdapterSourceMap[adapter.Key] = updatedPaths;
        }
        return discoveryCriteria;
    }

    public TestRunCriteriaWithSources UpdateTestRunCriteriaWithSources(TestRunCriteriaWithSources testRunCriteriaWithSources, PathConversionDirection updateDirection)
    {
        testRunCriteriaWithSources.AdapterSourceMap.Select(adapter => testRunCriteriaWithSources.AdapterSourceMap[adapter.Key] = UpdatePaths(adapter.Value, updateDirection));
        var package = UpdatePath(testRunCriteriaWithSources.Package, updateDirection);
        return new TestRunCriteriaWithSources(testRunCriteriaWithSources.AdapterSourceMap, package, testRunCriteriaWithSources.RunSettings, testRunCriteriaWithSources.TestExecutionContext);
    }

    public TestRunCriteriaWithTests UpdateTestRunCriteriaWithTests(TestRunCriteriaWithTests testRunCriteriaWithTests, PathConversionDirection updateDirection)
    {
        var tests = UpdateTestCases(testRunCriteriaWithTests.Tests, updateDirection);
        var package = UpdatePath(testRunCriteriaWithTests.Package, updateDirection);
        return new TestRunCriteriaWithTests(tests, package, testRunCriteriaWithTests.RunSettings, testRunCriteriaWithTests.TestExecutionContext);
    }
}
