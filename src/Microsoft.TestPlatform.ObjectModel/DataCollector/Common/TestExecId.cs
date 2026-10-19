// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

/// <summary>
/// Class identifying test execution id.
/// Execution ID is assigned to test at run creation time and is guaranteed to be unique within that run.
/// </summary>
[DataContract]
public sealed class TestExecId
{
    public TestExecId()
    {
        Id = Guid.NewGuid();
    }

    public TestExecId(Guid id)
    {
        Id = id;
    }

    [DataMember]
    public static TestExecId Empty { get; } = new TestExecId(Guid.Empty);

    [DataMember]
    public Guid Id { get; }

    public override bool Equals(object? obj) => obj is TestExecId testExecId && Id.Equals(testExecId.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => Id.ToString("B");

    /// <summary>
    /// Compares the two test execution IDs for value equality.
    /// </summary>
    /// <param name="left">The left-hand test execution ID</param>
    /// <param name="right">The right-hand test execution ID</param>
    /// <returns>True if equal, false otherwise</returns>
    public static bool operator ==(TestExecId? left, TestExecId? right)
    {
        return
            ReferenceEquals(left, right) ||
            left is not null &&
            right is not null &&
            left.Id == right.Id;
    }

    /// <summary>
    /// Compares the two test execution IDs for value inequality.
    /// </summary>
    /// <param name="left">The left-hand test execution ID</param>
    /// <param name="right">The right-hand test execution ID</param>
    /// <returns>True if unequal, false otherwise</returns>
    public static bool operator !=(TestExecId? left, TestExecId? right)
    {
        return !(left == right);
    }
}
