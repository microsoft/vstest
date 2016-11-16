// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System.Diagnostics;

    /// <summary>
    /// Session Start event arguments
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class SessionStartEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the session</param>
        public SessionStartEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }

    /// <summary>
    /// Session End event arguments
    /// </summary>
#if NET451
        [Serializable] 
#endif
    public sealed class SessionEndEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the session</param>
        public SessionEndEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }
    /// <summary>
    /// Session Pause event arguments
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class SessionPauseEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the session</param>
        public SessionPauseEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }

    /// <summary>
    /// Session Resume event arguments
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class SessionResumeEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the session</param>
        public SessionResumeEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }
}
