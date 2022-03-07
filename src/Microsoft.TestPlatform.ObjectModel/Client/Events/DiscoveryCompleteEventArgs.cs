// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

/// <summary>
/// Event arguments used on completion of discovery
/// </summary>
[DataContract]
public class DiscoveryCompleteEventArgs : EventArgs
{
    /// <summary>
    /// Constructor for creating event args object
    /// </summary>
    /// <param name="totalTests">Total tests which got discovered</param>
    /// <param name="isAborted">Specifies if discovery has been aborted.</param>
    /// <param name="fullyDiscoveredSources">List of fully discovered sources</param>
    /// <param name="partiallyDiscoveredSources">List of partially discovered sources</param>
    /// <param name="notDiscoveredSources">List of not discovered sources</param>
    public DiscoveryCompleteEventArgs(
        long totalTests,
        bool isAborted,
        IList<string> fullyDiscoveredSources,
        IList<string> partiallyDiscoveredSources,
        IList<string> notDiscoveredSources)
        : this(
              totalTests,
              isAborted,
              fullyDiscoveredSources,
              partiallyDiscoveredSources,
              notDiscoveredSources,
              new Dictionary<string, HashSet<string>>())
    { }

    /// <summary>
    /// Constructor for creating event args object
    /// </summary>
    /// <param name="totalTests">Total tests which got discovered</param>
    /// <param name="isAborted">Specifies if discovery has been aborted.</param>
    /// <param name="fullyDiscoveredSources">List of fully discovered sources</param>
    /// <param name="partiallyDiscoveredSources">List of partially discovered sources</param>
    /// <param name="notDiscoveredSources">List of not discovered sources</param>
    /// <param name="discoveredExtensions">Map containing discovered extensions.</param>
    public DiscoveryCompleteEventArgs(
        long totalTests,
        bool isAborted,
        IList<string> fullyDiscoveredSources,
        IList<string> partiallyDiscoveredSources,
        IList<string> notDiscoveredSources,
        Dictionary<string, HashSet<string>> discoveredExtensions)
    {
        TotalCount = totalTests;
        IsAborted = isAborted;

        FullyDiscoveredSources = fullyDiscoveredSources ?? new List<string>();
        PartiallyDiscoveredSources = partiallyDiscoveredSources ?? new List<string>();
        NotDiscoveredSources = notDiscoveredSources ?? new List<string>();

        DiscoveredExtensions = discoveredExtensions;
    }

    /// <summary>
    /// Constructor for creating event args object
    /// </summary>
    /// <param name="totalTests">Total tests which got discovered</param>
    /// <param name="isAborted">Specifies if discovery has been aborted.</param>
    public DiscoveryCompleteEventArgs(long totalTests, bool isAborted)
        : this(totalTests, isAborted, null, null, null)
    {
    }

    /// <summary>
    ///   Indicates the total tests which got discovered in this request.
    /// </summary>
    [DataMember]
    public long TotalCount { get; private set; }

    /// <summary>
    /// Specifies if discovery has been aborted. If true TotalCount is also set to -1.
    /// </summary>
    [DataMember]
    public bool IsAborted { get; private set; }

    /// <summary>
    /// Metrics
    /// </summary>
    [DataMember]
    public IDictionary<string, object> Metrics { get; set; }

    /// <summary>
    /// Gets the list of sources which were fully discovered.
    /// </summary>
    [DataMember]
    public IList<string> FullyDiscoveredSources { get; set; }

    /// <summary>
    /// Gets the list of sources which were partially discovered (started discover tests, but then discovery aborted).
    /// </summary>
    [DataMember]
    public IList<string> PartiallyDiscoveredSources { get; set; }

    /// <summary>
    /// Gets the list of sources which were not discovered at all.
    /// </summary>
    [DataMember]
    public IList<string> NotDiscoveredSources { get; set; }

    /// <summary>
    /// Gets or sets the collection of discovered extensions.
    /// </summary>
    [DataMember]
    public Dictionary<string, HashSet<string>> DiscoveredExtensions { get; set; }
}
