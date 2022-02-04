// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using XML;

/// <summary>
/// The test entry.
/// </summary>
internal sealed class TestEntry : IXmlTestStore
{
    #region Fields

    private readonly TestId _testId;
    private readonly TestListCategoryId _categoryId;
    private List<TestEntry> _testEntries;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor.
    /// Note that using this constructor has different effect as setting CategoryId property.
    /// When using this constructor, catId is used as specified, which CategoryId.set changes null to the root cat.
    /// </summary>
    /// <param name="testId">Test Id.</param>
    /// <param name="catId">Category Id. This gets into .</param>
    public TestEntry(TestId testId, TestListCategoryId catId)
    {
        _testId = testId;
        _categoryId = catId;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the exec id.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the parent exec id.
    /// </summary>
    public Guid ParentExecutionId { get; set; }

    public List<TestEntry> TestEntries
    {
        get
        {
            if (_testEntries == null)
            {
                _testEntries = new List<TestEntry>();
            }

            return _testEntries;
        }
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Override function for Equals.
    /// </summary>
    /// <param name="obj">
    /// The object to compare.
    /// </param>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public override bool Equals(object obj)
    {
        if (obj is not TestEntry e)
        {
            return false;
        }

        if (!ExecutionId.Equals(e.ExecutionId))
        {
            return false;
        }

        Debug.Assert(Equals(_testId, e._testId));
        Debug.Assert(Equals(_categoryId, e._categoryId));
        return true;
    }

    /// <summary>
    /// Override function for GetHashCode.
    /// </summary>
    /// <returns>
    /// The <see cref="int"/>.
    /// </returns>
    public override int GetHashCode()
    {
        return ExecutionId.GetHashCode();
    }

    #endregion

    #region IXmlTestStore Members

    /// <summary>
    /// Saves the class under the XmlElement..
    /// </summary>
    /// <param name="element">
    /// The parent xml.
    /// </param>
    /// <param name="parameters">
    /// The parameters.
    /// </param>
    public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
    {
        XmlPersistence helper = new();
        helper.SaveSingleFields(element, this, parameters);

        helper.SaveObject(_testId, element, null);
        helper.SaveGuid(element, "@executionId", ExecutionId);
        if (ParentExecutionId != Guid.Empty)
            helper.SaveGuid(element, "@parentExecutionId", ParentExecutionId);
        helper.SaveGuid(element, "@testListId", _categoryId.Id);
        if (TestEntries.Count > 0)
            helper.SaveIEnumerable(TestEntries, element, "TestEntries", ".", "TestEntry", parameters);
    }

    #endregion
}
