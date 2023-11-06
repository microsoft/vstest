// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Text;

namespace NuGetClone
{
    /// <summary>
    /// Provides a resource pool that enables reusing instances of <see cref="StringBuilder"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Renting and returning buffers with an <see cref="StringBuilderPool"/> can increase performance
    /// in situations where <see cref="StringBuilder"/> instances are created and destroyed frequently,
    /// resulting in significant memory pressure on the garbage collector.
    /// </para>
    /// <para>
    /// This class is thread-safe.  All members may be used by multiple threads concurrently.
    /// </para>
    /// </remarks>
    internal class StringBuilderPool
    {
        private const int MaxPoolSize = 256;
        private readonly SimplePool<StringBuilder> _pool = new(() => new StringBuilder(MaxPoolSize));

        /// <summary>
        /// Retrieves a shared <see cref="StringBuilderPool"/> instance.
        /// </summary>
        public static readonly StringBuilderPool Shared = new();

        private StringBuilderPool()
        {
        }

        /// <summary>
        /// Retrieves a <see cref="StringBuilder"/> that is at least the requested length.
        /// </summary>
        /// <param name="minimumCapacity">The minimum capacity of the <see cref="StringBuilder"/> needed.</param>
        /// <returns>
        /// A <see cref="StringBuilder"/> that is at least <paramref name="minimumCapacity"/> in length.
        /// </returns>
        /// <remarks>
        /// This buffer is loaned to the caller and should be returned to the same pool via
        /// <see cref="ToStringAndReturn"/> so that it may be reused in subsequent usage of <see cref="Rent"/>.
        /// It is not a fatal error to not return a rented string builder, but failure to do so may lead to
        /// decreased application performance, as the pool may need to create a new instance to replace
        /// the one lost.
        /// </remarks>
        public StringBuilder Rent(int minimumCapacity)
        {
            if (minimumCapacity <= MaxPoolSize)
            {
                return _pool.Allocate();
            }

            return new StringBuilder(minimumCapacity);
        }

        /// <summary>
        /// Returns to the pool an array that was previously obtained via <see cref="Rent"/> on the same
        /// <see cref="StringBuilderPool"/> instance, returning the built string.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> previously obtained from <see cref="Rent"/> to return to the pool.
        /// </param>
        /// <remarks>
        /// Once a <see cref="StringBuilder"/> has been returned to the pool, the caller gives up all ownership
        /// of the instance and must not use it. The reference returned from a given call to <see cref="Rent"/>
        /// must only be returned via <see cref="ToStringAndReturn"/> once.  The default <see cref="StringBuilderPool"/>
        /// may hold onto the returned instance in order to rent it again, or it may release the returned instance
        /// if it's determined that the pool already has enough instances stored.
        /// </remarks>
        /// <returns>The string, built from <paramref name="builder"/>.</returns>
        public string ToStringAndReturn(StringBuilder builder)
        {
            string result = builder.ToString();

            if (builder.Capacity <= MaxPoolSize)
            {
                builder.Clear();
                _pool.Free(builder);
            }

            return result;
        }
    }
}
