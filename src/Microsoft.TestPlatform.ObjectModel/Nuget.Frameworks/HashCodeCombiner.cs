// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace NuGetClone.Shared
{
    /// <summary>
    /// Hash code creator, based on the original NuGet hash code combiner/ASP hash code combiner implementations
    /// </summary>
    internal ref struct HashCodeCombiner
    {
        // seed from String.GetHashCode()
        private const long Seed = 0x1505L;

        private long _combinedHash = Seed;

        public HashCodeCombiner()
        {
        }

        internal int CombinedHash => _combinedHash.GetHashCode();

        private void AddHashCode(int i)
        {
            _combinedHash = ((_combinedHash << 5) + _combinedHash) ^ i;
        }

        internal void AddObject(int i)
        {
            AddHashCode(i);
        }

        internal void AddObject(bool b)
        {
            AddHashCode(b ? 1 : 0);
        }

        internal void AddObject<T>(T? o, IEqualityComparer<T> comparer)
            where T : class
        {
            if (o != null)
            {
                AddHashCode(comparer.GetHashCode(o));
            }
        }

        internal void AddObject<T>(T? o)
            where T : class
        {
            if (o != null)
            {
                AddHashCode(o.GetHashCode());
            }
        }

        // Optimization: for value types, we can avoid boxing "o" by skipping the null check
        internal void AddStruct<T>(T? o)
            where T : struct
        {
            if (o.HasValue)
            {
                AddHashCode(o.GetHashCode());
            }
        }

        // Optimization: for value types, we can avoid boxing "o" by skipping the null check
        internal void AddStruct<T>(T o)
            where T : struct
        {
            AddHashCode(o.GetHashCode());
        }

        internal void AddStringIgnoreCase(string? s)
        {
            if (s != null)
            {
                AddHashCode(StringComparer.OrdinalIgnoreCase.GetHashCode(s));
            }
        }

        internal void AddSequence<T>(IEnumerable<T>? sequence) where T : notnull
        {
            if (sequence != null)
            {
                foreach (var item in sequence.NoAllocEnumerate())
                {
                    AddHashCode(item.GetHashCode());
                }
            }
        }

        internal void AddSequence<T>(T[]? array) where T : notnull
        {
            if (array != null)
            {
                foreach (var item in array)
                {
                    AddHashCode(item.GetHashCode());
                }
            }
        }

        internal void AddSequence<T>(IList<T>? list) where T : notnull
        {
            if (list != null)
            {
                foreach (var item in list.NoAllocEnumerate())
                {
                    AddHashCode(item.GetHashCode());
                }
            }
        }

        internal void AddSequence<T>(IReadOnlyList<T>? list) where T : notnull
        {
            if (list != null)
            {
                var count = list.Count;
                for (var i = 0; i < count; i++)
                {
                    AddHashCode(list[i].GetHashCode());
                }
            }
        }

        internal void AddUnorderedSequence<T>(IEnumerable<T>? list) where T : notnull
        {
            if (list != null)
            {
                int count = 0;
                int hashCode = 0;
                foreach (var item in list.NoAllocEnumerate())
                {
                    // XOR is commutative -- the order of operations doesn't matter
                    hashCode ^= item.GetHashCode();
                    count++;
                }
                AddHashCode(hashCode);
                AddHashCode(count);
            }
        }

        internal void AddUnorderedSequence<T>(IEnumerable<T>? list, IEqualityComparer<T> comparer) where T : notnull
        {
            if (list != null)
            {
                int count = 0;
                int hashCode = 0;
                foreach (var item in list.NoAllocEnumerate())
                {
                    // XOR is commutative -- the order of operations doesn't matter
                    hashCode ^= comparer.GetHashCode(item);
                    count++;
                }
                AddHashCode(hashCode);
                AddHashCode(count);
            }
        }

        internal void AddDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? dictionary)
            where TKey : notnull
            where TValue : notnull
        {
            if (dictionary != null)
            {
                int count = 0;
                int dictionaryHash = 0;

                foreach (var pair in dictionary.NoAllocEnumerate())
                {
                    int keyHash = pair.Key.GetHashCode();
                    int valHash = pair.Value.GetHashCode();
                    int pairHash = ((keyHash << 5) + keyHash) ^ valHash;

                    // XOR is commutative -- the order of operations doesn't matter
                    dictionaryHash ^= pairHash;
                    count++;
                }

                AddHashCode(dictionaryHash + count);
            }
        }

        /// <summary>
        /// Create a unique hash code for the given set of items
        /// </summary>
        internal static int GetHashCode<T1, T2>(T1 o1, T2 o2)
            where T1 : notnull
            where T2 : notnull
        {
            var combiner = new HashCodeCombiner();

            combiner.AddHashCode(o1.GetHashCode());
            combiner.AddHashCode(o2.GetHashCode());

            return combiner.CombinedHash;
        }

        /// <summary>
        /// Create a unique hash code for the given set of items
        /// </summary>
        internal static int GetHashCode<T1, T2, T3>(T1 o1, T2 o2, T3 o3)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
        {
            var combiner = new HashCodeCombiner();

            combiner.AddHashCode(o1.GetHashCode());
            combiner.AddHashCode(o2.GetHashCode());
            combiner.AddHashCode(o3.GetHashCode());

            return combiner.CombinedHash;
        }
    }
}
