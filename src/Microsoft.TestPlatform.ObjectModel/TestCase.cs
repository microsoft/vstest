// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using Microsoft.TestPlatform.Hashing;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Stores information about a test case.
/// </summary>
[DataContract]
public sealed class TestCase : TestObject
{
    /// <summary>
    /// Disables computing <see cref="Id"/> with xxHash128, falling back to the SHA1 ids the platform
    /// has always produced. Set to <see cref="XxHash128OptInValue"/> to opt in to the new ids.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Moving to xxHash128 changes the id of every test whose id is computed by the platform, which
    /// is a breaking change for anything that stored those ids - most notably Azure DevOps Test Case
    /// work item association. It is therefore introduced as opt-in first, so that a release exists in
    /// which the new algorithm can be evaluated against real data without changing anyone's ids, and
    /// becomes the default only in a later release.
    /// </para>
    /// <para>
    /// See <see cref="TestCaseIdAlgorithmResolver"/> for why this is an opt-out flag rather than a
    /// selector naming an algorithm.
    /// </para>
    /// </remarks>
    internal const string TestCaseIdAlgorithmFeatureFlag = TestCaseIdAlgorithmResolver.FeatureFlagName;

    /// <summary>
    /// The value of <see cref="TestCaseIdAlgorithmFeatureFlag"/> that opts in to the xxHash128 ids.
    /// </summary>
    internal const string XxHash128OptInValue = TestCaseIdAlgorithmResolver.OptInValue;

    private Guid _defaultId = Guid.Empty;
    private Guid _id;
    private string? _displayName;
    private string _fullyQualifiedName;
    private string _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCase"/> class.
    /// </summary>
    /// <remarks>This constructor doesn't perform any parameter validation, it is meant to be used for serialization."/></remarks>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TestCase()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        // TODO: Make private
        // Default constructor for Serialization.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCase"/> class.
    /// </summary>
    /// <param name="fullyQualifiedName">
    /// Fully qualified name of the test case.
    /// </param>
    /// <param name="executorUri">
    /// The Uri of the executor to use for running this test.
    /// </param>
    /// <param name="source">
    /// Test container source from which the test is discovered.
    /// </param>
    public TestCase(string fullyQualifiedName, Uri executorUri, string source)
    {
        ValidateArg.NotNullOrEmpty(fullyQualifiedName, nameof(fullyQualifiedName));
        ValidateArg.NotNullOrEmpty(source, nameof(source));

        _fullyQualifiedName = fullyQualifiedName;
        ExecutorUri = executorUri ?? throw new ArgumentNullException(nameof(executorUri));
        _source = source;
        LineNumber = -1;
        _defaultId = Guid.Empty;

        // Record how this test case's id is going to be produced. Construction is the right moment
        // for it: this instance's id has not been assigned, so it will be computed, and it will be
        // computed with this process's algorithm - which is fixed for the lifetime of the process,
        // so recording it now cannot disagree with the value Id eventually hashes with. Doing it
        // here rather than from the Id getter also keeps that getter free of side effects.
        //
        // The serialization constructor deliberately does not do this. A test case being
        // deserialized already carries whatever the sending process recorded, and a test case sent
        // by a vstest that predates this property must keep carrying nothing rather than have this
        // process invent an answer on its behalf.
        SetPropertyValue(TestCaseProperties.IdAlgorithm, IdAlgorithmName(IdAlgorithm));
    }
    /// <summary>
    /// LocalExtensionData which can be used by Adapter developers for local transfer of extended properties.
    /// Note that this data is available only for in-Proc execution, and may not be available for OutProc executors
    /// </summary>
    public object? LocalExtensionData { get; set; }

    /// <summary>
    /// Gets or sets the id of the test case.
    /// </summary>
    [DataMember]
    public Guid Id
    {
        get
        {
            if (_id == Guid.Empty)
            {
                if (_defaultId == Guid.Empty)
                {
                    _defaultId = GetTestId();
                }

                return _defaultId;
            }

            return _id;
        }

        set
        {
            _id = value;

            // An id that is assigned is an id the platform did not hash, so whatever this instance
            // recorded about hashing it no longer holds.
            //
            // This only ever downgrades a value this instance recorded for itself, which is what
            // keeps deserialization honest: a test case built by the serialization constructor
            // carries nothing here, so restoring its id cannot invent a claim its sender never made,
            // and the recorded value then arrives with the rest of the property bag. The platform
            // itself assigns an id in one place - the Microsoft.Testing.Platform converter, which
            // computes the hash in the runner rather than letting this type compute it - and that
            // is not self assignment, so it records the algorithm it used afterwards.
            if (GetPropertyValue<string?>(TestCaseProperties.IdAlgorithm, null) is not null)
            {
                SetPropertyValue(TestCaseProperties.IdAlgorithm, TestCaseIdAlgorithms.SelfAssigned);
            }
        }
    }

    /// <summary>
    /// Gets or sets the fully qualified name of the test case.
    /// </summary>
    [DataMember]
    public string FullyQualifiedName
    {
        get => _fullyQualifiedName;

        // defaultId should be reset as it is based on FullyQualifiedName and Source.
        set => SetVariableAndResetId(ref _fullyQualifiedName, value);
    }

    /// <summary>
    /// Gets or sets the display name of the test case.
    /// </summary>
    [DataMember]
    public string DisplayName
    {
        get => _displayName.IsNullOrEmpty() ? GetFullyQualifiedName() : _displayName;
        set => _displayName = value;
    }

    /// <summary>
    /// Gets or sets the Uri of the Executor to use for running this test.
    /// </summary>
    [DataMember]
    public Uri ExecutorUri
    {
        get; set;
    }

    /// <summary>
    /// Gets the test container source from which the test is discovered.
    /// </summary>
    [DataMember]
    public string Source
    {
        get => _source;
        set
        {
            _source = value;

            // defaultId should be reset as it is based on FullyQualifiedName and Source.
            _defaultId = Guid.Empty;
        }
    }

    /// <summary>
    /// Gets or sets the source code file path of the test.
    /// </summary>
    [DataMember]
    public string? CodeFilePath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the line number of the test.
    /// </summary>
    [DataMember]
    public int LineNumber
    {
        get; set;
    }

    /// <summary>
    /// Returns the TestProperties currently specified in this TestObject.
    /// </summary>
    public override IEnumerable<TestProperty> Properties
    {
        get
        {
            return TestCaseProperties.Properties.Concat(base.Properties);
        }
    }

    /// <summary>
    /// The algorithm used to compute test case ids in this process.
    /// </summary>
    /// <remarks>
    /// Resolved from the <see cref="TestCaseIdAlgorithmFeatureFlag"/> feature flag, which reads the
    /// environment lazily on first use and then caches it. Both properties matter here: reading
    /// lazily keeps the choice observable from a test rather than baking it in whenever the type
    /// happened to be loaded, and caching keeps an id stable for the lifetime of the process.
    /// </remarks>
    private static TestCaseIdAlgorithm IdAlgorithm => TestCaseIdAlgorithmResolver.Ambient;

    /// <summary>
    /// The value <see cref="TestCaseProperties.IdAlgorithm"/> carries for <paramref name="algorithm"/>.
    /// </summary>
    private static string IdAlgorithmName(TestCaseIdAlgorithm algorithm)
        => algorithm switch
        {
            TestCaseIdAlgorithm.XxHash128 => TestCaseIdAlgorithms.XxHash128,
            TestCaseIdAlgorithm.Sha1 => TestCaseIdAlgorithms.Sha1,

            // Naming both members above means adding a third one surfaces here as a deliberate
            // decision rather than being silently reported as SHA1.
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };

    /// <summary>
    /// Records that the platform computed <see cref="Id"/> with <paramref name="algorithm"/>, for a
    /// caller that hashed the id itself and assigned it rather than letting this type compute it.
    /// </summary>
    /// <remarks>
    /// Only the Microsoft.Testing.Platform path does that, because there the runner builds the test
    /// case and so has to hash it in a process that is not the one the test ran in. Such an id is
    /// platform computed with a known algorithm, not self assigned, and assigning <see cref="Id"/>
    /// has just recorded the opposite - so this has to be called after it, not before.
    /// </remarks>
    internal void SetIdAlgorithm(TestCaseIdAlgorithm algorithm)
        => SetPropertyValue(TestCaseProperties.IdAlgorithm, IdAlgorithmName(algorithm));

    /// <summary>
    /// Clears every cached feature flag, so <see cref="IdAlgorithm"/> reads its own again. For tests
    /// only - production code must not change algorithm mid-process.
    /// </summary>
    /// <remarks>
    /// Named for what it actually does rather than for the one flag this type cares about: it resets
    /// the whole <see cref="FeatureFlag"/> cache, because that is the granularity available. Flags
    /// that are pure environment reads are simply read again, but any value a test injected with
    /// <c>FeatureFlag.SetFlag</c> is discarded, so a caller has to know the scope. It is exposed here
    /// rather than used directly because CoreUtilities internals are not visible to the ObjectModel
    /// test assembly, and it carries the same <see cref="ObsoleteAttribute"/> the method it forwards
    /// to carries, so a production assembly that can see it cannot call it without saying so.
    /// </remarks>
    [Obsolete("Only use this in tests.")]
#pragma warning disable CS0618 // FeatureFlag.Reset exists for tests, which is what this is for.
    internal static void ResetFeatureFlagCacheForTesting() => FeatureFlag.Reset();
#pragma warning restore CS0618

    /// <summary>
    /// Creates a Id of TestCase
    /// </summary>
    /// <returns>Guid test id</returns>
    private Guid GetTestId() => GetTestId(IdAlgorithm);

    /// <summary>
    /// Creates a Id of TestCase using the given algorithm.
    /// </summary>
    /// <returns>Guid test id</returns>
    private Guid GetTestId(TestCaseIdAlgorithm algorithm)
    {
        // To generate id hash "ExecutorUri + source + Name". The composition lives in TestIdSeed
        // because the Microsoft.Testing.Platform path has to reproduce it exactly from the runner
        // process, where a TestCase is built rather than computed. If ManagedType and ManagedMethod
        // properties are filled then the id is based on those, which is what GetFullyQualifiedName
        // resolves.
        // ExecutorUri is passed as text rather than concatenated directly: the original expression
        // concatenated the Uri object, which renders a null as empty, and a test case built through
        // the serialization constructor can still be missing it.
        string testcaseFullName = TestIdSeed.Compose(ExecutorUri?.ToString(), Source, GetFullyQualifiedName());

        return algorithm switch
        {
            // GuidFromStringXxHash128 is experimental. This call is deliberate: it is how the flag
            // opts a run into the algorithm, and it is in the same assembly that declares it.
#pragma warning disable VSTEST001
            TestCaseIdAlgorithm.XxHash128 => EqtHash.GuidFromStringXxHash128(testcaseFullName),
#pragma warning restore VSTEST001
            TestCaseIdAlgorithm.Sha1 => EqtHash.GuidFromString(testcaseFullName),

            // Naming both members above means adding a third one surfaces here as a deliberate
            // decision rather than silently resolving to SHA1.
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };
    }

    private void SetVariableAndResetId<T>(ref T variable, T value)
    {
        variable = value;
        _defaultId = Guid.Empty;
    }

    private void SetPropertyAndResetId<T>(TestProperty property, T value)
    {
        SetPropertyValue(property, value);
        _defaultId = Guid.Empty;
    }

    /// <summary>
    /// Return TestProperty's value
    /// </summary>
    /// <returns></returns>
    protected override object? ProtectedGetPropertyValue(TestProperty property, object? defaultValue)
    {
        ValidateArg.NotNull(property, nameof(property));
        return property.Id switch
        {
            "TestCase.CodeFilePath" => CodeFilePath,
            "TestCase.DisplayName" => DisplayName,
            "TestCase.ExecutorUri" => ExecutorUri,
            "TestCase.FullyQualifiedName" => FullyQualifiedName,
            "TestCase.Id" => Id,
            "TestCase.LineNumber" => LineNumber,
            "TestCase.Source" => Source,
            // It is a custom test case property. Should be retrieved from the TestObject store.
            _ => base.ProtectedGetPropertyValue(property, defaultValue),
        };
    }

    /// <summary>
    /// Set TestProperty's value
    /// </summary>
    protected override void ProtectedSetPropertyValue(TestProperty property, object? value)
    {
        ValidateArg.NotNull(property, nameof(property));
        switch (property.Id)
        {
            case "TestCase.CodeFilePath":
                CodeFilePath = value as string;
                return;

            case "TestCase.DisplayName":
                DisplayName = (value as string)!;
                return;

            case "TestCase.ExecutorUri":
                ExecutorUri = value as Uri ?? new Uri((value as string)!);
                return;

            case "TestCase.FullyQualifiedName":
                FullyQualifiedName = (value as string)!;
                return;

            case "TestCase.Id":
                if (value is Guid guid)
                {
                    Id = guid;
                }
                else if (value is string guidString)
                {
                    Id = GuidPolyfill.Parse(guidString, CultureInfo.InvariantCulture);
                }
                else
                {
                    Id = Guid.Empty;
                }

                return;

            case "TestCase.LineNumber":
                LineNumber = (int)value!;
                return;

            case "TestCase.Source":
                Source = (value as string)!;
                return;
        }

        // It is a custom test case property. Should be set in the TestObject store.
        base.ProtectedSetPropertyValue(property, value);
    }

    private static readonly TestProperty ManagedTypeProperty = TestProperty.Register("TestCase.ManagedType", "ManagedType", string.Empty, string.Empty, typeof(string), o => !StringUtils.IsNullOrWhiteSpace(o as string), TestPropertyAttributes.Hidden, typeof(TestCase));
    private static readonly TestProperty ManagedMethodProperty = TestProperty.Register("TestCase.ManagedMethod", "ManagedMethod", string.Empty, string.Empty, typeof(string), o => !StringUtils.IsNullOrWhiteSpace(o as string), TestPropertyAttributes.Hidden, typeof(TestCase));

    private bool ContainsManagedMethodAndType => !StringUtils.IsNullOrWhiteSpace(ManagedMethod) && !StringUtils.IsNullOrWhiteSpace(ManagedType);

    private string? ManagedType
    {
        get => GetPropertyValue<string>(ManagedTypeProperty, null);
        set => SetPropertyAndResetId(ManagedTypeProperty, value);
    }

    private string? ManagedMethod
    {
        get => GetPropertyValue<string>(ManagedMethodProperty, null);
        set => SetPropertyAndResetId(ManagedMethodProperty, value);
    }

    internal string GetFullyQualifiedName() => ContainsManagedMethodAndType ? $"{ManagedType}.{ManagedMethod}" : FullyQualifiedName;

    /// <inheritdoc/>
    public override string ToString() => GetFullyQualifiedName();
}

/// <summary>
/// Well-known TestCase properties
/// </summary>
public static class TestCaseProperties
{
    /// <summary>
    /// These are the core Test properties and may be available in commandline/TeamBuild to filter tests.
    /// These Property names should not be localized.
    /// </summary>
    private const string IdLabel = "Id";
    private const string FullyQualifiedNameLabel = "FullyQualifiedName";
    private const string NameLabel = "Name";
    private const string ExecutorUriLabel = "Executor Uri";
    private const string SourceLabel = "Source";
    private const string FilePathLabel = "File Path";
    private const string LineNumberLabel = "Line Number";
    private const string IdAlgorithmLabel = "Id Algorithm";

    public static readonly TestProperty Id = TestProperty.Register("TestCase.Id", IdLabel, string.Empty, string.Empty, typeof(Guid), ValidateGuid, TestPropertyAttributes.Hidden, typeof(TestCase));
    public static readonly TestProperty FullyQualifiedName = TestProperty.Register("TestCase.FullyQualifiedName", FullyQualifiedNameLabel, string.Empty, string.Empty, typeof(string), ValidateName, TestPropertyAttributes.Hidden, typeof(TestCase));
    public static readonly TestProperty DisplayName = TestProperty.Register("TestCase.DisplayName", NameLabel, string.Empty, string.Empty, typeof(string), ValidateDisplay, TestPropertyAttributes.None, typeof(TestCase));
    public static readonly TestProperty ExecutorUri = TestProperty.Register("TestCase.ExecutorUri", ExecutorUriLabel, string.Empty, string.Empty, typeof(Uri), ValidateExecutorUri, TestPropertyAttributes.Hidden, typeof(TestCase));
    public static readonly TestProperty Source = TestProperty.Register("TestCase.Source", SourceLabel, typeof(string), typeof(TestCase));
    public static readonly TestProperty CodeFilePath = TestProperty.Register("TestCase.CodeFilePath", FilePathLabel, typeof(string), typeof(TestCase));
    public static readonly TestProperty LineNumber = TestProperty.Register("TestCase.LineNumber", LineNumberLabel, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    /// <summary>
    /// How the id a test case carries was produced: one of the values on
    /// <see cref="TestCaseIdAlgorithms"/>, or absent when the test case comes from a vstest that
    /// predates this property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vstest computes <see cref="TestCase.Id"/> by hashing a seed string, and which hash it uses is
    /// changing - see <c>docs/environment-variables.md</c>. When it changes, the id of every test
    /// whose id the platform computes changes with it, and a consumer that cached those ids cannot
    /// match its cache against the results it gets back. Reading this tells such a consumer that the
    /// ids it holds were produced a different way and have to be discovered again, rather than
    /// leaving it to notice that nothing matches.
    /// </para>
    /// <para>
    /// Deliberately not one of the core properties above. It is an ordinary entry in the property
    /// bag, so it travels on every protocol version through the mechanism that already carries
    /// custom properties, needs no change to any serializer, and is simply absent - rather than
    /// wrong - on a payload written before it existed.
    /// </para>
    /// </remarks>
    public static readonly TestProperty IdAlgorithm = TestProperty.Register("TestCase.IdAlgorithm", IdAlgorithmLabel, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static TestProperty[] Properties { get; } =
    [
        // IdAlgorithm is deliberately absent. This array is the set of core properties that a
        // serializer writes as fields of its own, and that TestCase answers from a backing field
        // rather than from the property bag. IdAlgorithm lives in the bag instead, which is what
        // makes it travel on every protocol version without a serializer knowing about it.
        CodeFilePath,
        DisplayName,
        ExecutorUri,
        FullyQualifiedName,
        Id,
        LineNumber,
        Source
    ];

    private static bool ValidateName(object? value)
    {
        return !StringUtils.IsNullOrWhiteSpace((string?)value);
    }

    private static bool ValidateDisplay(object? value)
    {
        // only check for null and pass the rest up to UI for validation
        return value != null;
    }

    private static bool ValidateExecutorUri(object? value)
    {
        return value != null;
    }

    private static bool ValidateGuid(object? value)
    {
        if (value?.ToString() is not string sValue)
        {
            return false;
        }

        // TODO: Replace with TryParse?
        try
        {
            _ = new Guid(sValue);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}

/// <summary>
/// The values <see cref="TestCaseProperties.IdAlgorithm"/> can carry.
/// </summary>
/// <remarks>
/// <para>
/// A test case that carries none of these carries nothing at all: it was produced by a vstest that
/// predates the property, and a consumer has to fall back on whatever it assumed before.
/// </para>
/// <para>
/// The vocabulary matches the <c>IdSource</c> column of the test id report logger - see
/// <c>docs/test-ids-logger.md</c> - on purpose, because the two answer the same question. The logger
/// infers it by comparing a test's id against both candidates, which is all a report written after
/// the fact can do; this is stated by the code that produced the id, so it is right even for an
/// adapter that assigns an id which happens to collide with a computed one.
/// </para>
/// </remarks>
public static class TestCaseIdAlgorithms
{
    /// <summary>
    /// The platform computed the id by hashing the test case's seed with SHA1. This is the id
    /// vstest has always produced by default.
    /// </summary>
    public const string Sha1 = "Sha1";

    /// <summary>
    /// The platform computed the id by hashing the test case's seed with xxHash128.
    /// </summary>
    public const string XxHash128 = "XxHash128";

    /// <summary>
    /// The adapter assigned the id itself and the platform hashed nothing, so the id does not move
    /// when the platform changes the algorithm it hashes with. MSTest v3 and later do this.
    /// </summary>
    public const string SelfAssigned = "SelfAssigned";
}
