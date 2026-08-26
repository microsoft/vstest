// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

/// <summary>
/// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper"/>.
/// </summary>
///
/// <remarks>
/// The whole interface is obsolete: none of the asynchronous operations it declares actually run
/// asynchronously, so consumers should use the synchronous <see cref="IVsTestConsoleWrapper"/> API instead.
/// The interface-level attribute flags direct references to this type; the per-member attributes are kept
/// deliberately, because the C# compiler does not report the interface-level obsoletion when a member is
/// invoked through the derived <see cref="IVsTestConsoleWrapper"/>, which is how virtually every consumer
/// reaches these methods.
/// <para>
/// The interface-level attribute is new, so code that names this type directly gets a warning it did not get
/// before, on every target framework. That includes the two <c>ProcessTestRunAttachmentsAsync</c> overloads
/// below, which stay undeprecated because they have no synchronous replacement; a consumer that only needs
/// those should hold an <see cref="IVsTestConsoleWrapper"/> rather than this interface. Nothing here is a
/// binary breaking change and every attribute uses <c>error: false</c>.
/// </para>
/// <para>
/// <c>ObsoleteAttribute.DiagnosticId</c> only exists on .NET 5 and newer, so the TPVS001 id is applied under
/// <c>#if NET</c>. Consumers of the .NET Framework and netstandard2.0 assemblies keep getting plain CS0618
/// rather than TPVS001.
/// </para>
/// </remarks>
#if NET
[Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
[Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
public interface IVsTestConsoleWrapperAsync
{
    /// <summary>
    /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.StartSession"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task StartSessionAsync();

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.InitializeExtensions(
    ///     IEnumerable{string})"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task InitializeExtensionsAsync(IEnumerable<string> pathToAdditionalExtensions);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.DiscoverTests(
    ///     IEnumerable{string},
    ///     string,
    ///     ITestDiscoveryEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? discoverySettings,
        ITestDiscoveryEventsHandler discoveryEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.DiscoverTests(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     ITestDiscoveryEventsHandler2)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? discoverySettings,
        TestPlatformOptions? options,
        ITestDiscoveryEventsHandler2 discoveryEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.DiscoverTests(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestDiscoveryEventsHandler2)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task DiscoverTestsAsync(
        IEnumerable<string> sources,
        string? discoverySettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestDiscoveryEventsHandler2 discoveryEventsHandler);

    /// <summary>
    /// See <see cref="IVsTestConsoleWrapper.CancelDiscovery"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    void CancelDiscovery();

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{string},
    ///     string,
    ///     ITestRunEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     ITestRunEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler,
    ///     ITelemetryEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     ITestRunEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    ///     IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     TestPlatformOptions,
    ///     ITestRunEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    ///     IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    ///     IVsTestConsoleWrapper.RunTests(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler,
    ///     ITelemetryEventsHandler)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{string},
    ///     string,
    ///     ITestRunEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     ITestRunEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{string},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler,
    ///     ITelemetryEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<string> sources,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     ITestRunEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     TestPlatformOptions,
    ///     ITestRunEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// Asynchronous equivalent of <see cref="
    /// IVsTestConsoleWrapper.RunTestsWithCustomTestHost(
    ///     IEnumerable{TestCase},
    ///     string,
    ///     TestPlatformOptions,
    ///     TestSessionInfo,
    ///     ITestRunEventsHandler,
    ///     ITelemetryEventsHandler,
    ///     ITestHostLauncher)"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    Task RunTestsWithCustomTestHostAsync(
        IEnumerable<TestCase> testCases,
        string? runSettings,
        TestPlatformOptions? options,
        TestSessionInfo? testSessionInfo,
        ITestRunEventsHandler testRunEventsHandler,
        ITelemetryEventsHandler telemetryEventsHandler,
        ITestHostLauncher customTestHostLauncher);

    /// <summary>
    /// See <see cref="IVsTestConsoleWrapper.CancelTestRun"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    void CancelTestRun();

    /// <summary>
    /// See <see cref="IVsTestConsoleWrapper.AbortTestRun"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    void AbortTestRun();

    /// <summary>
    /// Gets back all attachments to test platform for additional processing (for example merging).
    /// </summary>
    ///
    /// <param name="attachments">Collection of attachments.</param>
    /// <param name="processingSettings">XML processing settings.</param>
    /// <param name="isLastBatch">
    /// Indicates that all test executions are done and all data is provided.
    /// </param>
    /// <param name="collectMetrics">Enables metrics collection (used for telemetry).</param>
    /// <param name="eventsHandler">Event handler to receive session complete event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessTestRunAttachmentsAsync(
        IEnumerable<AttachmentSet> attachments,
        string? processingSettings,
        bool isLastBatch,
        bool collectMetrics,
        ITestRunAttachmentsProcessingEventsHandler eventsHandler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets back all attachments to test platform for additional processing (for example merging).
    /// </summary>
    ///
    /// <param name="attachments">Collection of attachments.</param>
    /// <param name="invokedDataCollectors">Collection of invoked data collectors.</param>
    /// <param name="processingSettings">XML processing settings.</param>
    /// <param name="isLastBatch">
    /// Indicates that all test executions are done and all data is provided.
    /// </param>
    /// <param name="collectMetrics">Enables metrics collection (used for telemetry).</param>
    /// <param name="eventsHandler">Event handler to receive session complete event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessTestRunAttachmentsAsync(
        IEnumerable<AttachmentSet> attachments,
        IEnumerable<InvokedDataCollector>? invokedDataCollectors,
        string? processingSettings,
        bool isLastBatch,
        bool collectMetrics,
        ITestRunAttachmentsProcessingEventsHandler eventsHandler,
        CancellationToken cancellationToken);

    /// <summary>
    /// See <see cref="IVsTestConsoleWrapper.EndSession"/>.
    /// </summary>
#if NET
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false, DiagnosticId = "TPVS001", UrlFormat = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md#tpvs001")]
#else
    [Obsolete("The async APIs don't work, use the sync API instead.", error: false)]
#endif
    void EndSession();
}
