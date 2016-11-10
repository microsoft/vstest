// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;

    /// <summary>
    /// Base class for all data collector custom notification event arguments
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public abstract class CustomNotificationEventArgs : DataCollectionEventArgs
    {
        /// <summary>
        /// Initializes for sending a session level custom notification.
        /// </summary>
        protected CustomNotificationEventArgs() :
            this(new DataCollectionContext(SessionId.Empty, null), null)
        {
        }

        /// <summary>
        /// Initializes for sending a test case level custom notification
        /// </summary>
        /// <param name="testExecId">ID of the test case to send the event against.</param>
        protected CustomNotificationEventArgs(TestExecId testExecId) :
            base(new DataCollectionContext(SessionId.Empty, testExecId))
        {

        }

        /// <summary>
        /// Initializes for sending a custom notification against the provided data collection context.
        /// </summary>
        /// <param name="context">Data Collection Context that the event is being sent against.</param>
        internal CustomNotificationEventArgs(DataCollectionContext context) :
            this(context, null)
        {
        }

        /// <summary>
        /// Initializes for sending a custom notification against the provided data collection context.
        /// </summary>
        /// <param name="context">Data Collection Context that the event is being sent against.</param>
        /// <param name="targetedUri">Uri of the targetd collector</param>
        internal CustomNotificationEventArgs(DataCollectionContext context, Uri targetDataCollectorUri) :
            base(context, targetDataCollectorUri)
        {
            this.NotificationIdentifier = Guid.NewGuid();
        }

        /// <summary>
        /// Identifier for this custom notification
        /// </summary>
        internal Guid NotificationIdentifier
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Custom data send from the collector
    /// </summary>
#if NET451
    [Serializable]
#endif
    public abstract class CustomCollectorData
    {
    }

#if NET451
    [Serializable] 
#endif
    public class CustomCollectorGenericErrorData : CustomCollectorData
    {
        public string Message
        {
            get;
            set;
        }
    }
}
