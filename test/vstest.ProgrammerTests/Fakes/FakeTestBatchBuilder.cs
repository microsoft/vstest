﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



#pragma warning disable IDE1006 // Naming Styles
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace vstest.ProgrammerTests.CommandLine;

internal class FakeTestBatchBuilder
{
    public int TotalCount { get; private set; }
    public TimeSpan Duration { get; private set; }
    public int BatchSize { get; private set; }

    public FakeTestBatchBuilder()
    {
    }

    /// <summary>
    /// Total test count in all batches.
    /// </summary>
    internal FakeTestBatchBuilder WithTotalCount(int count)
    {
        TotalCount = count;
        return this;
    }

    internal FakeTestBatchBuilder WithDuration(TimeSpan duration)
    {

        // TODO: add min duration and max duration, and distribution, if timing becomes relevant
        // TODO: and replay rate, if we actually want to simulate stuff like really executing the tests
        Duration = duration;
        return this;
    }

    /// <summary>
    /// Splits the tests to batches of this size when reporting them back.
    /// </summary>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    internal FakeTestBatchBuilder WithBatchSize(int batchSize)
    {
        BatchSize = batchSize;
        return this;
    }

    internal List<List<TestResult>> Build()
    {
        var numberOfBatches = Math.DivRem(TotalCount, BatchSize, out int remainder);

        // TODO: Add adapter uri, and dll name
        // TODO: set duration
        var batches =
            Enumerable.Range(0, numberOfBatches)
            .Select(batchNumber =>
                Enumerable.Range(0, BatchSize)
                .Select((index) => new TestResult(new TestCase($"Test{batchNumber}-{index}", new Uri("some://uri"), "DummySourceFileName"))).ToList()).ToList();

        if (remainder > 0)
        {
            var reminderBatch = Enumerable.Range(0, BatchSize)
                .Select((index) => new TestResult(new TestCase($"Test{numberOfBatches + 1}-{index}", new Uri("some://uri"), "DummySourceFileName"))).ToList();

            batches.Add(reminderBatch);
        }

        return batches;
    }
}
