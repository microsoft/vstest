// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    /// <summary>
    /// Custom Guid converter class
    /// </summary>
    internal class CustomGuidConverter : GuidConverter
    {
        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(Guid))
            {
                return new Guid(value.ToString());
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
