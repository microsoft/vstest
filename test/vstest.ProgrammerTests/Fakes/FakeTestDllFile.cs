// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using System.Runtime.Versioning;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestDllFile : FakeFile
{
    public FrameworkName FrameworkName { get; }
    public Architecture Architecture { get; }
    public List<List<TestResult>> TestResultBatches { get; }

    public FakeTestDllFile(string path, FrameworkName frameworkName, Architecture architecture, List<List<TestResult>> testResultBatches) : base(path)
    {
        FrameworkName = frameworkName;
        Architecture = architecture;
        TestResultBatches = testResultBatches;
    }
}
