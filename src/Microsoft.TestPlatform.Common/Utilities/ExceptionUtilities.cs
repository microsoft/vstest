// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System;

    /// <summary>
    /// Exception utilities.
    /// </summary>
    public class ExceptionUtilities
    {
        /// <summary>
        /// Returns an exception message with all inner exceptions messages.
        /// </summary>
        /// <param name="exception"> The exception. </param>
        /// <returns> The formatted string message of the exception. </returns>
        public static string GetExceptionMessage(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var exceptionString = exception.Message;
            var inner = exception.InnerException;
            while (inner != null)
            {
                exceptionString += Environment.NewLine + inner.Message;
                inner = inner.InnerException;
            }

            return exceptionString;
        }
    }
}
