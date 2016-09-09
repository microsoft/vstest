// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;

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
            var strings = value as string[];
            if (strings != null)
            {
                return strings;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // String[] are used by adapters. E.g. TestCategory[]
            var data = value as string;
            if (data != null)
            {
                using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(data)))
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(string[]));
                    var strings = serializer.ReadObject(stream) as string[];

                    return strings;
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}