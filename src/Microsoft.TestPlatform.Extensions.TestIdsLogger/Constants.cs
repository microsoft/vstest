// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.TestIdsLogger;

/// <summary>
/// Constants identifying the test id report logger and its parameters.
/// </summary>
/// <remarks>
/// <para>
/// TEMPORARY - THIS LOGGER WILL BE REMOVED. It exists only to support migrating stored test case
/// ids across the change of id hashing algorithm, and is deleted together with the SHA1
/// implementation it reports on. See the class level remarks on <see cref="TestIdsLogger"/>.
/// </para>
/// </remarks>
public static class Constants
{
    /// <summary>
    /// Uri used to uniquely identify the test id report logger.
    /// </summary>
    public const string ExtensionUri = "logger://Microsoft/TestPlatform/TestIdsLogger/v1";

    /// <summary>
    /// Alternate user friendly string to uniquely identify the test id report logger.
    /// </summary>
    public const string FriendlyName = "testids";

    /// <summary>
    /// Log file parameter key, holding the path the report is written to. An absolute path is used
    /// as given, a relative one is resolved against the test results directory.
    /// </summary>
    public const string LogFileNameKey = "LogFileName";

    /// <summary>
    /// The report file name used when <see cref="LogFileNameKey"/> is not given and the platform
    /// reported no target framework.
    /// </summary>
    /// <remarks>
    /// Deliberately a fixed name rather than a timestamped one: the report is an input to a
    /// migration script, and a script that has to glob for its own input is worse than one that
    /// overwrites a known path. Internal rather than public, because a real run almost always
    /// carries a target framework and so almost always qualifies the name with it - a constant that
    /// names a file that is usually not there is worse than no constant at all.
    /// </remarks>
    internal const string DefaultReportFileName = DefaultReportFileNameWithoutExtension + ReportFileExtension;

    /// <summary>
    /// The stem of <see cref="DefaultReportFileName"/>, to which the target framework is appended.
    /// </summary>
    internal const string DefaultReportFileNameWithoutExtension = "TestIds";

    /// <summary>
    /// The extension of the report file.
    /// </summary>
    internal const string ReportFileExtension = ".csv";

    /// <summary>
    /// Property id of the managed type a test case carries when its adapter reports one.
    /// </summary>
    /// <remarks>
    /// Matched by id against the properties present on the test case rather than looked up in the
    /// global property store, because whether the property has been registered in this process
    /// depends on load order that a logger has no control over.
    /// </remarks>
    internal const string ManagedTypePropertyId = "TestCase.ManagedType";

    /// <summary>
    /// Property id of the managed method a test case carries when its adapter reports one.
    /// </summary>
    internal const string ManagedMethodPropertyId = "TestCase.ManagedMethod";
}
