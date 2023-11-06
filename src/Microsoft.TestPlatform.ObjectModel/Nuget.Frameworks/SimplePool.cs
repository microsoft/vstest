// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;

namespace NuGetClone
{
    internal class SimplePool<T> where T : class
    {
        private readonly ConcurrentStack<T> _values = new();
        private readonly Func<T> _allocate;

        public SimplePool(Func<T> allocate)
        {
            _allocate = allocate;
        }

        public T Allocate()
        {
            if (_values.TryPop(out T? result))
            {
                return result;
            }

            return _allocate();
        }

        public void Free(T value)
        {
            _values.Push(value);
        }
    }
}
