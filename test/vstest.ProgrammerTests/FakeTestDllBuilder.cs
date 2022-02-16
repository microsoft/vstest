﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using System;
using System.Runtime.Versioning;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

internal class FakeTestDllBuilder
{
    private string _path = @$"X:\fake\mstest_{Guid.NewGuid()}.dll";
    private FrameworkName _framework = KnownFramework.Net50;
    private Architecture _architecture = Architecture.X64;
    private List<List<TestResult>>? _testBatches;

    internal FakeTestDllBuilder WithFramework(FrameworkName framework)
    {
        _framework = framework;
        return this;
    }

    internal FakeTestDllBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    internal FakeTestDllBuilder WithArchitecture(Architecture architecture)
    {
        _architecture = architecture;
        return this;
    }

    /// <summary>
    /// Use this together with TestBatchBuilder, or use WithTestCount to get basic test batch.
    /// </summary>
    /// <param name="testBatches"></param>
    /// <returns></returns>
    internal FakeTestDllBuilder WithTestBatches(List<List<TestResult>> testBatches)
    {
        _testBatches = testBatches;
        return this;
    }

    /// <summary>
    /// Use this to get basic test batch, or use WithTestBatches together with TestBatchBuilder, to get a custom batch.
    /// </summary>
    /// <param name="testBatches"></param>
    /// <returns></returns>
    internal FakeTestDllBuilder WithTestCount(int totalCount, int? batchSize = null)
    {
        _testBatches = new FakeTestBatchBuilder()
            .WithTotalCount(totalCount)
            .WithBatchSize(batchSize ?? totalCount)
            .Build();

        return this;
    }

    internal FakeTestDllFile Build()
    {
        if (_testBatches == null)
        {
            _testBatches = new FakeTestBatchBuilder()
            .WithTotalCount(10)
            .WithBatchSize(5)
            .Build();
        }
        return new FakeTestDllFile(_path, _framework, _architecture, _testBatches);
    }
}
