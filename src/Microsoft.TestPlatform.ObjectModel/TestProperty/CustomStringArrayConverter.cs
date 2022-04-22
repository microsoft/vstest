// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

internal class CustomStringArrayConverter : TypeConverter
{
    private readonly DataContractJsonSerializer _serializer;

    private readonly Dictionary<string, string[]> _memoization = new();
    public CustomStringArrayConverter ()
    {
        _serializer = new DataContractJsonSerializer(typeof(string[]));
    }

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <inheritdoc/>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return value is string[] strings ? strings : base.ConvertTo(context, culture, value, destinationType);
    }

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        // String[] are used by adapters. E.g. TestCategory[]
        if (value is string data)
        {
            if (_memoization.TryGetValue(data, out string[] str))
            {
                return str;
            }

            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            var strings = _serializer.ReadObject(stream) as string[];
            _memoization[data] = strings;
            return strings;
        }

        return null;
    }
}
