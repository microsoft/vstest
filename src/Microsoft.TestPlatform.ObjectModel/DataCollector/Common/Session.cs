// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    
    /// <summary>
    /// Class identifying a session.
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class SessionId
    {
        private Guid sessionId;

        private static SessionId empty = new SessionId(Guid.Empty);

        public SessionId()
        {
            sessionId = Guid.NewGuid();
        }

        public SessionId(Guid id)
        {
            sessionId = id;
        }

        public static SessionId Empty
        {
            get { return empty; }
        }

        public Guid Id
        {
            get { return sessionId; }
        }

        public override bool Equals(object obj)
        {
            SessionId id = obj as SessionId;

            if (id == null)
            {
                return false;
            }

            return sessionId.Equals(id.sessionId);
        }

        public override int GetHashCode()
        {
            return sessionId.GetHashCode();
        }

        public override string ToString()
        {
            return sessionId.ToString("B");
        }
    }
}