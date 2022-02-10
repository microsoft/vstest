// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Converts a json representation of <see cref="KeyValuePair{String,String}"/> to an object.
/// </summary>
internal class CustomKeyValueConverter : TypeConverter
{
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
            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            // Converting Json data to array of KeyValuePairs with duplicate keys.
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TraitObject[]));
            var listOfTraitObjects = serializer.ReadObject(stream) as TraitObject[];

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
