// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Converts paths in received and sent objects, to make testhost seem like it run a local test,
/// while it was in fact running a test on a remote system, in a totally different path. This is for UWP which
/// does testhost deployment.
/// The modifications here rely on combination of side-effects, and actually replacing the values, because
/// we cannot modify the properties on our public objects, and add setters.
/// </summary>
internal class PathConverter : IPathConverter
{
    // The path on this computer to which we deployed the test dll and test runner
    private readonly string _deploymentPath = "";
    // The path on the remote system where test dll was originally placed, and from which we
    // copied it to this system. For vstest.console, which is on the other side of this, the names
    // are inverted, it sends us their local path, and thinks about our local path as remote.
    private readonly string _originalPath = "";

    public PathConverter(string originalPath!!, string deploymentPath!!, IFileHelper fileHelper!!)
    {
        _originalPath = fileHelper.GetFullPath(originalPath).TrimEnd('\\').TrimEnd('/') + Path.DirectorySeparatorChar;
        _deploymentPath = fileHelper.GetFullPath(deploymentPath).TrimEnd('\\').TrimEnd('/') + Path.DirectorySeparatorChar;
    }

    public string? UpdatePath(string? path, PathConversionDirection updateDirection)
    {
        if (path == null)
            return path;

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

    public IEnumerable<string?> UpdatePaths(IEnumerable<string?> paths!!, PathConversionDirection updateDirection)
    {
        return paths.Select(i => UpdatePath(i, updateDirection)).ToList();
    }

    public TestCase UpdateTestCase(TestCase testCase!!, PathConversionDirection updateDirection)
    {
        testCase.CodeFilePath = UpdatePath(testCase.CodeFilePath, updateDirection);
        testCase.Source = UpdatePath(testCase.Source, updateDirection);
        return testCase;
    }

    public IEnumerable<TestCase> UpdateTestCases(IEnumerable<TestCase> testCases!!, PathConversionDirection updateDirection)
    {
        testCases.ToList().ForEach(tc => UpdateTestCase(tc, updateDirection));
        return testCases;
    }

    public TestRunCompleteEventArgs UpdateTestRunCompleteEventArgs(TestRunCompleteEventArgs testRunCompleteEventArgs!!, PathConversionDirection updateDirection)
    {
        UpdateAttachmentSets(testRunCompleteEventArgs.AttachmentSets, updateDirection);
        return testRunCompleteEventArgs;
    }

    public TestRunChangedEventArgs UpdateTestRunChangedEventArgs(TestRunChangedEventArgs testRunChangedArgs!!, PathConversionDirection updateDirection)
    {
        UpdateTestResults(testRunChangedArgs.NewTestResults, updateDirection);
        UpdateTestCases(testRunChangedArgs.ActiveTests, updateDirection);
        return testRunChangedArgs;
    }

    public Collection<AttachmentSet> UpdateAttachmentSets(Collection<AttachmentSet> attachmentSets!!, PathConversionDirection updateDirection)
    {
        attachmentSets.ToList().ForEach(i => UpdateAttachmentSet(i, updateDirection));
        return attachmentSets;
    }

    public ICollection<AttachmentSet> UpdateAttachmentSets(ICollection<AttachmentSet> attachmentSets!!, PathConversionDirection updateDirection)
    {
        attachmentSets.ToList().ForEach(i => UpdateAttachmentSet(i, updateDirection));
        return attachmentSets;
    }

    private AttachmentSet UpdateAttachmentSet(AttachmentSet attachmentSet!!, PathConversionDirection updateDirection)
    {
        attachmentSet.Attachments.ToList().ForEach(a => UpdateAttachment(a, updateDirection));
        return attachmentSet;
    }

    private UriDataAttachment UpdateAttachment(UriDataAttachment attachment!!, PathConversionDirection _)
    {
        // todo: convert uri? https://github.com/microsoft/vstest/issues/3367
        return attachment;
    }

    private IEnumerable<TestResult> UpdateTestResults(IEnumerable<TestResult> testResults!!, PathConversionDirection updateDirection)
    {
        foreach (var tr in testResults)
        {
            UpdateAttachmentSets(tr.Attachments, updateDirection);
            UpdateTestCase(tr.TestCase, updateDirection);
        }
        return testResults;
    }

    public DiscoveryCriteria UpdateDiscoveryCriteria(DiscoveryCriteria discoveryCriteria!!, PathConversionDirection updateDirection)
    {
        discoveryCriteria.Package = UpdatePath(discoveryCriteria.Package, updateDirection);
        foreach (var adapter in discoveryCriteria.AdapterSourceMap.ToList())
        {
            var updatedPaths = UpdatePaths(adapter.Value, updateDirection);
            discoveryCriteria.AdapterSourceMap[adapter.Key] = updatedPaths;
        }
        return discoveryCriteria;
    }

    public TestRunCriteriaWithSources UpdateTestRunCriteriaWithSources(TestRunCriteriaWithSources testRunCriteriaWithSources!!, PathConversionDirection updateDirection)
    {
        testRunCriteriaWithSources.AdapterSourceMap.ToList().ForEach(adapter => testRunCriteriaWithSources.AdapterSourceMap[adapter.Key] = UpdatePaths(adapter.Value, updateDirection));
        var package = UpdatePath(testRunCriteriaWithSources.Package, updateDirection);
        return new TestRunCriteriaWithSources(testRunCriteriaWithSources.AdapterSourceMap, package, testRunCriteriaWithSources.RunSettings, testRunCriteriaWithSources.TestExecutionContext);
    }

    public TestRunCriteriaWithTests UpdateTestRunCriteriaWithTests(TestRunCriteriaWithTests testRunCriteriaWithTests!!, PathConversionDirection updateDirection)
    {
        var tests = UpdateTestCases(testRunCriteriaWithTests.Tests, updateDirection);
        var package = UpdatePath(testRunCriteriaWithTests.Package, updateDirection);
        return new TestRunCriteriaWithTests(tests, package, testRunCriteriaWithTests.RunSettings, testRunCriteriaWithTests.TestExecutionContext);
    }
}
