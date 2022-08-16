// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel;

/// <summary>
/// Stores the list of failed results and list of all results grouped by class name.
/// </summary>
[DataContract]
public class TestResultByClass
{
    private readonly string _className;

    public TestResultByClass(string source) => _className = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>
    /// Source of the test dll.
    /// </summary>
    [DataMember]
    public string ClassName
    {
        get => _className;
        private set { }
    }

    /// <summary>
    /// Hash id of ClassName.
    /// </summary>
    [DataMember]
    public int UniqueId
    {
        get => _className.GetHashCode();
        private set { }
    }

    /// <summary>
    /// List of test results.
    /// </summary>
    [DataMember] public List<TestResult>? ResultList { get; set; }

    /// <summary>
    /// List of failed test results.
    /// </summary>
    [DataMember] public List<TestResult>? FailedResultList { get; set; }
}
