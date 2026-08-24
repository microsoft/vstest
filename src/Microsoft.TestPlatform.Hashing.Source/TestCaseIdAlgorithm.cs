// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.TestPlatform.Hashing;

/// <summary>
/// The algorithms available for computing a test case id.
/// </summary>
internal enum TestCaseIdAlgorithm
{
    /// <summary>
    /// The SHA1 based ids, unversioned. These are the ids vstest computes unless a run selects
    /// otherwise, and every id it has produced by default historically.
    /// </summary>
    Sha1,

    /// <summary>
    /// xxHash128 based ids, carrying the hashing scheme version in the produced GUID.
    /// </summary>
    XxHash128,
}

/// <summary>
/// Resolves which algorithm computes a test case id.
/// </summary>
/// <remarks>
/// <para>
/// Shared source, compiled into <c>Microsoft.TestPlatform.ObjectModel</c>, where <c>TestCase</c>
/// computes its own id. <c>Microsoft.TestPlatform.CrossPlatEngine</c>, where the
/// Microsoft.Testing.Platform path has to compute the id in the runner process, uses that same
/// definition through <c>InternalsVisibleTo</c>, so both resolve the same value from the same input.
/// </para>
/// <para>
/// This is deliberately an algorithm selector rather than a boolean. A boolean would have to be
/// named after whichever algorithm happens to be the default at the time, so changing the default
/// would invert the meaning of every value users had already written down. A selector names the
/// algorithm instead, which makes a value written today keep meaning the same thing after the
/// default moves, and lets a future scheme be added without inventing a second switch.
/// </para>
/// </remarks>
internal static class TestCaseIdAlgorithmResolver
{
    /// <summary>
    /// The environment variable that selects the algorithm.
    /// </summary>
    /// <remarks>
    /// Read by whichever process builds the test case: the testhost on the classic path, the runner
    /// on the Microsoft.Testing.Platform path. It can be set in the environment directly, or declared
    /// in runsettings under <c>RunConfiguration/EnvironmentVariables</c>.
    /// </remarks>
    public const string EnvironmentVariableName = "VSTEST_TESTCASE_ID_ALGORITHM";

    /// <summary>
    /// The value selecting <see cref="TestCaseIdAlgorithm.Sha1"/>.
    /// </summary>
    public const string Sha1Name = "sha1";

    /// <summary>
    /// The value selecting <see cref="TestCaseIdAlgorithm.XxHash128"/>.
    /// </summary>
    public const string XxHash128Name = "xxhash128";

    /// <summary>
    /// The algorithm used when a run does not select one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the whole rollout in one constant. xxHash128 ships available but not default, so the
    /// release that introduces it changes no id at all and can be adopted with zero risk; changing
    /// this constant is then the entire behavioural change of the release that follows, rather than
    /// a diffuse change of polarity spread over the classic path, the Microsoft.Testing.Platform
    /// path and their tests.
    /// </para>
    /// <para>
    /// Changing it will fail the pinned expectations that record what ids actually are - the ids in
    /// TestCaseTests, the serialized payloads in TestResultSerializationTests, and the test that
    /// asserts this default directly. That is the intended blast radius rather than a bug: those are
    /// exactly the places whose whole job is to notice that ids moved.
    /// </para>
    /// <para>
    /// Both names are understood in both releases, so a value written down while this is
    /// <see cref="TestCaseIdAlgorithm.Sha1"/> keeps selecting exactly the same algorithm afterwards:
    /// someone pinning <c>sha1</c> today is unaffected by the change, and an early adopter of
    /// <c>xxhash128</c> does not have to unset anything once it becomes the default.
    /// </para>
    /// </remarks>
    public const TestCaseIdAlgorithm Default = TestCaseIdAlgorithm.Sha1;

    /// <summary>
    /// Resolves the algorithm a declared value selects, falling back to <see cref="Default"/>.
    /// </summary>
    /// <remarks>
    /// An unrecognized value falls back to the default rather than throwing. This is read on the way
    /// to computing an id, at a point where there is nowhere sensible to surface an error, and
    /// failing a whole run over a typo in an opt-in switch would be a worse outcome than ignoring it.
    /// </remarks>
    public static TestCaseIdAlgorithm Resolve(string? declaredValue)
        => Parse(declaredValue) ?? Default;

    /// <summary>
    /// Parses a declared value, returning <see langword="null"/> when it selects no known algorithm.
    /// </summary>
    public static TestCaseIdAlgorithm? Parse(string? declaredValue)
    {
        if (string.Equals(declaredValue, Sha1Name, StringComparison.OrdinalIgnoreCase))
        {
            return TestCaseIdAlgorithm.Sha1;
        }

        if (string.Equals(declaredValue, XxHash128Name, StringComparison.OrdinalIgnoreCase))
        {
            return TestCaseIdAlgorithm.XxHash128;
        }

        return null;
    }

    /// <summary>
    /// Resolves the algorithm declared by a set of environment variables, typically the ones a run
    /// declares in runsettings <c>RunConfiguration/EnvironmentVariables</c>.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when the variable is not declared at all, so the caller can fall back
    /// to the ambient environment of whichever process ends up computing the id. Declaring the
    /// variable wins, even when its value is not recognized, so a run that says something explicit
    /// about the algorithm is never silently overridden by an inherited value.
    /// </returns>
    public static TestCaseIdAlgorithm? ResolveDeclared(IDictionary<string, string?>? environmentVariables)
    {
        if (environmentVariables is null)
        {
            return null;
        }

        foreach (KeyValuePair<string, string?> variable in environmentVariables)
        {
            // The dictionary is not necessarily keyed case-insensitively, and on non-Windows the
            // classic path keys it case-sensitively on purpose, so match the name rather than index.
            if (string.Equals(variable.Key, EnvironmentVariableName, StringComparison.OrdinalIgnoreCase))
            {
                return Resolve(variable.Value);
            }
        }

        return null;
    }
}
