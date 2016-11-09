// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
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
            var keyValuePairs = value as KeyValuePair<string, string>[];
            if (keyValuePairs != null)
            {
                return keyValuePairs;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // KeyValuePairs are used for traits. 
            var data = value as string;
            if (data != null)
            {
                using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(data)))
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Dictionary<string, string>));
                    var dict = serializer.ReadObject(stream) as Dictionary<string, string>;

                    return dict?.ToArray();
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}