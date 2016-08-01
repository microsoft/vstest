// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
{
    using System;

#if NET46
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// Exception thrown by the framework when an executor attempts to send 
    /// test result to the framework when the test is canceled.  
    /// </summary>
#if NET46
    [Serializable]
#endif
    public class TestCanceledException : Exception
    {
#region Constructors

        /// <summary>
        /// Creates a new TestCanceledException
        /// </summary>
        public TestCanceledException()
        {
        }

        /// <summary>
        /// Initializes with the message.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        public TestCanceledException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes with message and inner exception.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public TestCanceledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if NET46
        /// <summary>
        /// Seralization constructor.
        /// </summary>
        protected TestCanceledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
#endregion
    }
}
