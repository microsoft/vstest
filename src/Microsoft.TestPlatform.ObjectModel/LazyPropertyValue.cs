// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    /// <summary>
    /// Represents a lazy value calculation for a TestObject
    /// </summary>
    internal interface ILazyPropertyValue
    {
        /// <summary>
        /// Forces calculation of the value
        /// </summary>
        object Value { get; }
    }

    /// <summary>
    /// Represents a lazy value calculation for a TestObject
    /// </summary>
    /// <typeparam name="T">The type of the value to be calculated</typeparam>
    public sealed class LazyPropertyValue<T> : ILazyPropertyValue
    {
        private T value;
        private readonly Func<T> getValue;
        private bool isValueCreated;

        public LazyPropertyValue(Func<T> getValue)
        {
            isValueCreated = false;
            value = default;
            this.getValue = getValue;
        }

        /// <summary>
        /// Forces calculation of the value
        /// </summary>
        public T Value
        {
            get
            {
                if (!isValueCreated)
                {
                    value = getValue();
                    isValueCreated = true;
                }

                return value;
            }
        }

        object ILazyPropertyValue.Value
        {
            get
            {
                return Value;
            }
        }
    }
}
