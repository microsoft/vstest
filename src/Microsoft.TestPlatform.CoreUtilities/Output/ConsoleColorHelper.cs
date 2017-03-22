// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;

    /// <summary>
    /// Console color helper
    /// </summary>
    public sealed class ConsoleColorHelper
    {
        /// <summary>
        /// Set given foregroundColor to Console.ForegroundColor during given action.
        /// </summary>
        /// <param name="foregroundColor"></param>
        /// <param name="action"></param>
        public static void SetColorForAction(ConsoleColor foregroundColor, Action action)
        {
            if(action == null)
            {
                return;
            }

            var previousForegroundColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = foregroundColor;
                action.Invoke();
            }
            finally
            {
                Console.ForegroundColor = previousForegroundColor;
            }
        }
    }
}
