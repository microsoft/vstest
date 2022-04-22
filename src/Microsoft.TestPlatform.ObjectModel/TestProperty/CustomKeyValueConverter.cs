// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Converts a json representation of <see cref="KeyValuePair{String,String}"/> to an object.
/// </summary>
internal class CustomKeyValueConverter : TypeConverter
{
    private readonly DataContractJsonSerializer _serializer;

    private readonly Dictionary<string, KeyValuePair<string, string>[]> _memoization = new();

    public CustomKeyValueConverter()
    {
        _serializer = new DataContractJsonSerializer(typeof(TraitObject[]));
    }

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <inheritdoc/>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return value is KeyValuePair<string, string>[] keyValuePairs
            ? keyValuePairs
            : base.ConvertTo(context, culture, value, destinationType);
    }

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        // KeyValuePairs are used for traits. 
        if (value is string data)
        {
            if (_memoization.TryGetValue(data, out KeyValuePair<string, string>[] traits))
            {
                return traits;
            }

            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            // Converting Json data to array of KeyValuePairs with duplicate keys.
            var listOfTraitObjects = _serializer.ReadObject(stream) as TraitObject[];
            _memoization[data] = traits;
            return listOfTraitObjects.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).ToArray();
        }

        return null;
    }

    [System.Runtime.Serialization.DataContract]
    private class TraitObject
    {
        [System.Runtime.Serialization.DataMember(Name = "Key")]
        public string Key { get; set; }

        [System.Runtime.Serialization.DataMember(Name = "Value")]
        public string Value { get; set; }
    }
}
