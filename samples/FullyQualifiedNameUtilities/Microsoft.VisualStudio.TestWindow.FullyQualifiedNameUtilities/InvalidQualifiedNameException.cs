using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.TestWindow.FullyQualifiedNameUtilities
{
    [Serializable]
    public class InvalidQualifiedNameException : Exception
    {
        public InvalidQualifiedNameException(string msg) : base(msg) { }
        public InvalidQualifiedNameException(SerializationInfo si, StreamingContext ctx) : base(si, ctx) { }
    }
}
