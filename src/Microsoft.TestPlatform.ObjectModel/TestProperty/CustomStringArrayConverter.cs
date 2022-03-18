// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

internal class CustomStringArrayConverter : TypeConverter
{
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
            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(string[]));
            var strings = serializer.ReadObject(stream) as string[];

            return strings;
        }

        return null;
    }
}
