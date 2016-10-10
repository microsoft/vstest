// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;

    /// <summary>
    /// Colors the console output and restores it when disposed.
    /// </summary>
    public sealed class ConsoleColorHelper : IDisposable
    {
        private readonly ConsoleColor previousForegroundColor;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleColorHelper"/> class.
        /// </summary>
        /// <param name="foregroundColor">
        /// Color to set the console foreground to.
        /// </param>
        public ConsoleColorHelper(ConsoleColor foregroundColor)
        {
            this.previousForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
        }

        /// <summary>
        /// Restores the original foreground color.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the composition container.
        /// </summary>
        /// <param name="disposing">True if the object is disposing.</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Console.ForegroundColor = this.previousForegroundColor;
                }

                this.disposed = true;
            }
        }
    }
}
