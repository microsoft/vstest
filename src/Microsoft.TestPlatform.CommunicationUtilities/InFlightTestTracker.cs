// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Tracks a set of in-flight test executions that share the same <see cref="Guid"/>.
/// Uses inline slots to avoid queue allocation in the common case (unique Guid per test).
/// All mutation and enumeration is guarded by a lock because the message-receive thread
/// updates slots while the abort thread may read them concurrently via <see cref="GetAll"/>.
/// </summary>
internal sealed class InFlightTest
{
    private readonly object _lock = new();
    public TestCaseStartingPayload Slot0;
    public DateTimeOffset StartTime0;
    public TestCaseStartingPayload? Slot1;
    public DateTimeOffset StartTime1;
    public TestCaseStartingPayload? Slot2;
    public DateTimeOffset StartTime2;
    public TestCaseStartingPayload? Slot3;
    public DateTimeOffset StartTime3;
    public Queue<(TestCaseStartingPayload Payload, DateTimeOffset StartTime)>? Overflow;

    public InFlightTest(TestCaseStartingPayload payload, DateTimeOffset startTime)
    {
        Slot0 = payload;
        StartTime0 = startTime;
    }

    /// <summary>
    /// Adds another in-flight execution for the same Guid.
    /// </summary>
    public void Add(TestCaseStartingPayload payload, DateTimeOffset startTime)
    {
        lock (_lock)
        {
            if (Slot1 is null)
            {
                Slot1 = payload;
                StartTime1 = startTime;
            }
            else if (Slot2 is null)
            {
                Slot2 = payload;
                StartTime2 = startTime;
            }
            else if (Slot3 is null)
            {
                Slot3 = payload;
                StartTime3 = startTime;
            }
            else
            {
                Overflow ??= new Queue<(TestCaseStartingPayload, DateTimeOffset)>();
                Overflow.Enqueue((payload, startTime));
            }
        }
    }

    /// <summary>
    /// Removes the oldest in-flight execution (FIFO). Returns true if there are still entries remaining.
    /// </summary>
    public bool RemoveOldest()
    {
        lock (_lock)
        {
            // Shift slots down
            if (Slot1 is not null)
            {
                Slot0 = Slot1;
                StartTime0 = StartTime1;
                Slot1 = Slot2;
                StartTime1 = StartTime2;
                Slot2 = Slot3;
                StartTime2 = StartTime3;
                Slot3 = null;
                StartTime3 = default;

                // Refill Slot3 from overflow if available
                if (Overflow is { Count: > 0 })
                {
                    var (payload, startTime) = Overflow.Dequeue();
                    Slot3 = payload;
                    StartTime3 = startTime;
                }

                return true;
            }

            // Slot0 was the only entry
            return false;
        }
    }

    /// <summary>
    /// Returns a snapshot of all in-flight entries with their display name and start time.
    /// </summary>
    public IReadOnlyList<(string? DisplayName, DateTimeOffset StartTime)> GetAll()
    {
        lock (_lock)
        {
            var result = new List<(string?, DateTimeOffset)>();
            result.Add((Slot0.DisplayName, StartTime0));

            if (Slot1 is not null)
            {
                result.Add((Slot1.DisplayName, StartTime1));
            }

            if (Slot2 is not null)
            {
                result.Add((Slot2.DisplayName, StartTime2));
            }

            if (Slot3 is not null)
            {
                result.Add((Slot3.DisplayName, StartTime3));
            }

            if (Overflow is not null)
            {
                foreach (var (payload, startTime) in Overflow)
                {
                    result.Add((payload.DisplayName, startTime));
                }
            }

            return result;
        }
    }
}

/// <summary>
/// Thread-safe tracker for tests currently executing in a testhost.
/// Used by vstest.console to report which tests were running when a testhost crashes.
/// </summary>
internal sealed class InFlightTestTracker
{
    private readonly ConcurrentDictionary<Guid, InFlightTest> _tests = new();

    /// <summary>
    /// Records that a test has started executing.
    /// </summary>
    public void TestStarting(TestCaseStartingPayload payload, DateTimeOffset startTime)
    {
        _tests.AddOrUpdate(
            payload.Id,
            _ => new InFlightTest(payload, startTime),
            (_, existing) =>
            {
                existing.Add(payload, startTime);
                return existing;
            });
    }

    /// <summary>
    /// Records that a test has finished executing. Removes the oldest execution for the given Guid.
    /// </summary>
    public void TestFinished(Guid testId)
    {
        if (_tests.TryGetValue(testId, out var inFlight))
        {
            if (!inFlight.RemoveOldest())
            {
                _tests.TryRemove(testId, out _);
            }
        }
    }

    /// <summary>
    /// Gets all tests that are currently in-flight. Used when testhost crashes to report what was running.
    /// </summary>
    public IReadOnlyList<(string? DisplayName, DateTimeOffset StartTime)> GetInFlightTests()
    {
        var result = new List<(string?, DateTimeOffset)>();
        foreach (var kvp in _tests)
        {
            foreach (var entry in kvp.Value.GetAll())
            {
                result.Add(entry);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns true if there are any in-flight tests being tracked.
    /// </summary>
    public bool HasInFlightTests => !_tests.IsEmpty;
}
