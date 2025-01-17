// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
using Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger;

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

#region TraitItem
/// <summary>
/// Stores a string which categorizes the Test
/// </summary>
internal sealed class Property : IXmlTestStore
{
    [StoreXmlSimpleField(Location = "@Key", DefaultValue = "")]
    private readonly string _key = string.Empty;
    [StoreXmlSimpleField(Location = "@Value", DefaultValue = "")]
    private readonly string _value = string.Empty;

    /// <summary>
    /// Create a new item with the category set
    /// </summary>
    public Property(string key, string value)
    {
        // Treat null as empty.

        _key = StripIllegalChars(key);
        _value = StripIllegalChars(value);
    }

    /// <summary>
    /// Gets the category for this Trait
    /// </summary>
    public string Trait
    {
        get
        {
            return _key;
        }
    }

    private static string StripIllegalChars(string category)
    {
        string ret = category.Trim();
        ret = ret.Replace("&", string.Empty);
        ret = ret.Replace("|", string.Empty);
        ret = ret.Replace("!", string.Empty);
        ret = ret.Replace(",", string.Empty);
        return ret;
    }

    /// <summary>
    /// Compare the values of the items
    /// </summary>
    /// <param name="other">Value being compared to.</param>
    /// <returns>True if the values are the same and false otherwise.</returns>
    public override bool Equals(object? other)
    {
        if (other is not Property otherItem)
        {
            return false;
        }

        TPDebug.Assert(_key != null, "category is null");
        return string.Equals(_key, otherItem._key, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Convert the category name to a hashcode
    /// </summary>
    /// <returns>Hashcode of the category.</returns>
    public override int GetHashCode()
    {
        TPDebug.Assert(_key != null, "category is null");
        return _key.ToUpperInvariant().GetHashCode();
    }

    /// <summary>
    /// Convert the category name to a string
    /// </summary>
    /// <returns>The category.</returns>
    public override string ToString()
    {
        TPDebug.Assert(_key != null, "category is null");
        return _key;
    }

    #region IXmlTestStore Members

    /// <summary>
    /// Saves the class under the XmlElement.
    /// </summary>
    /// <param name="element"> XmlElement element </param>
    /// <param name="parameters"> XmlTestStoreParameters parameters</param>
    public void Save(System.Xml.XmlElement element, XmlTestStoreParameters? parameters)
    {
        new XmlPersistence().SaveSingleFields(element, this, parameters);
    }

    #endregion
}
#endregion

#region TraitItemCollection
/// <summary>
/// A collection of strings which categorize the test.
/// </summary>
internal sealed class PropertyCollection : EqtBaseCollection<Property>
{
    /// <summary>
    /// Creates an empty TraitItemCollection.
    /// </summary>
    public PropertyCollection()
    {
    }

    /// <summary>
    /// Adds the category.
    /// </summary>
    /// <param name="key">Key to be added.</param>
    /// <param name="value">Value to be added.</param>
    public void Add(string key, string value)
    {
        Add(new Property(key, value));
    }

    /// <summary>
    /// Adds the category.
    /// </summary>
    /// <param name="item">Category to be added.</param>
    public override void Add(Property item)
    {
        EqtAssert.ParameterNotNull(item, nameof(item));

        // Don't add empty items.
        if (!item.Trait.IsNullOrEmpty())
        {
            base.Add(item);
        }
    }

    /// <summary>
    /// Convert the TraitItemCollection to a string.
    /// each item is surrounded by a comma (,)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder returnString = new();
        if (Count > 0)
        {
            returnString.Append(',');
            foreach (Property item in this)
            {
                returnString.Append(item.Trait);
                returnString.Append(',');
            }
        }

        return returnString.ToString();
    }

    /// <summary>
    /// Convert the TraitItemCollection to an array of strings.
    /// </summary>
    /// <returns>Array of strings containing the test categories.</returns>
    public string[] ToArray()
    {
        string[] result = new string[Count];

        int i = 0;
        foreach (Property item in this)
        {
            result[i++] = item.Trait;
        }

        return result;
    }

    /// <summary>
    /// Compare the collection items
    /// </summary>
    /// <param name="obj">other collection</param>
    /// <returns>true if the collections contain the same items</returns>
    public override bool Equals(object? obj)
    {
        bool result = false;

        if (obj is not PropertyCollection other)
        {
            // Other object is not a TraitItemCollection.
            result = false;
        }
        else if (ReferenceEquals(this, other))
        {
            // The other object is the same object as this one.
            result = true;
        }
        else if (Count != other.Count)
        {
            // The count of categories in the other object does not
            // match this one, so they are not equal.
            result = false;
        }
        else
        {
            // Check each item and return on the first mismatch.
            foreach (Property item in this)
            {
                if (!other.Contains(item))
                {
                    result = false;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Return the hash code of this collection
    /// </summary>
    /// <returns>The hashcode.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
#endregion
