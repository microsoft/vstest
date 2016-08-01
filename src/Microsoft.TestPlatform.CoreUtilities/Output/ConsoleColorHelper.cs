// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;

    /// <summary>
    /// Colors the console output and restores it when disposed.
    /// </summary>
    public sealed class ConsoleColorHelper : IDisposable
    {
        #region Fields

        private bool m_isDisposed;
        private ConsoleColor m_previousForgroundColor;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes with the color to set the console forground to.
        /// </summary>
        /// <param name="forgroundColor">Color to set the console forground to.</param>
        public ConsoleColorHelper(ConsoleColor foregroundColor)
        {
            m_previousForgroundColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Restores the original forground color.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the composition container.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!m_isDisposed)
            {
                if (disposing)
                {
                    Console.ForegroundColor = m_previousForgroundColor;
                }

                m_isDisposed = true;
            }
        }

        #endregion
    }
}
