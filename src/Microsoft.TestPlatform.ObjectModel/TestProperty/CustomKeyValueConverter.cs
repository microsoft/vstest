// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#if NET7_0_OR_GREATER
using System.Text.Json.Serialization;
#endif
#if !NET7_0_OR_GREATER
using System.Runtime.Serialization.Json;
#endif

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Converts a json representation of <see cref="KeyValuePair{String,String}"/> to an object.
/// </summary>
internal partial class CustomKeyValueConverter : TypeConverter
{
#if NET7_0_OR_GREATER // TODO: We actually don't yet produce net7.0 in the NuGet package.
    [JsonSerializable(typeof(TraitObject[]))]
    private partial class TraitObjectSerializerContext : JsonSerializerContext
    {
    }
#else
    private readonly DataContractJsonSerializer _serializer = new(typeof(TraitObject[]));
#endif

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return value is KeyValuePair<string, string>[] keyValuePairs
            ? keyValuePairs
            : base.ConvertTo(context, culture, value, destinationType);
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        // KeyValuePairs are used for traits.
        if (value is string data)
        {
            // PERF: The values returned here can possibly be cached, but the benefits are very small speed wise,
            // and it is unclear how many distinct objects we get, and how much memory this would consume. I was seeing around 100ms improvement on 10k tests.

            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            // Converting Json data to array of KeyValuePairs with duplicate keys.
#if NET7_0_OR_GREATER
            var listOfTraitObjects = System.Text.Json.JsonSerializer.Deserialize(stream, TraitObjectSerializerContext.Default.TraitObjectArray);
#else
            var listOfTraitObjects = _serializer.ReadObject(stream) as TraitObject[];
#endif
            return listOfTraitObjects?.Select(trait => new KeyValuePair<string?, string?>(trait.Key, trait.Value)).ToArray() ?? [];
        }

        return null;
    }

    [System.Runtime.Serialization.DataContract]
    private class TraitObject
    {
        [System.Runtime.Serialization.DataMember(Name = "Key")]
        public string? Key { get; set; }

        [System.Runtime.Serialization.DataMember(Name = "Value")]
        public string? Value { get; set; }
    }
}
