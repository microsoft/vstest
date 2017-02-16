// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// Wrapper class for a request ID that can be used for messages or events for identification
    /// purposes
    /// </summary>
#if NET46
    [Serializable]
#endif
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes",
        Justification = "Guid does not define < and > operators")]
    public sealed class RequestId : IEquatable<RequestId>, IComparable<RequestId>, IComparable
    {
        #region Constants

        /// <summary>
        /// A request ID with an empty GUID
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "RequestId is immutable")]
        public static readonly RequestId Empty = new RequestId(Guid.Empty);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the instance by creating a new GUID
        /// </summary>
        internal RequestId()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes the instance with the provided GUID
        /// </summary>
        /// <param name="id">The GUID to use as the underlying ID</param>
        internal RequestId(Guid id)
        {
            Id = id;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Compares this instance with the provided object for value equality
        /// </summary>
        /// <param name="obj">The object to compare to</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            RequestId other = obj as RequestId;
            if (other == null)
            {
                return false;
            }

            return Id == other.Id;
        }

        /// <summary>
        /// Gets a hash code for this instance
        /// </summary>
        /// <returns>The underlying GUID's hash code</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Converts the instance to a string in lower-case registry format
        /// </summary>
        /// <returns>A lower-case string in registry format representing the underlying GUID</returns>
        public override string ToString()
        {
            return Id.ToString("B");
        }

        #endregion

        #region Interface implementations

        #region IEquatable<RequestId> Members

        /// <summary>
        /// Compares this instance with the provided request ID for value equality
        /// </summary>
        /// <param name="other">The request ID to compare to</param>
        /// <returns>True if equal, false otherwise</returns>
        public bool Equals(RequestId other)
        {
            return
                other != null && (
                        object.ReferenceEquals(this, other) ||
                        Id == other.Id
                    );
        }

        #endregion

        #region IComparable<RequestId> Members

        /// <summary>
        /// Compares this instance with the provided request ID
        /// </summary>
        /// <param name="other">The request ID to compare to</param>
        /// <returns>An indication of the two request IDs' relative values</returns>
        public int CompareTo(RequestId other)
        {
            return other == null ? 1 : Id.CompareTo(other.Id);
        }

        #endregion

        #region IComparable Members

        /// <summary>
        /// Compares this instance with the provided object
        /// </summary>
        /// <param name="obj">The object to compare to</param>
        /// <returns>An indication of the two objects' relative values</returns>
        /// <exception cref="ArgumentException">
        /// 'obj' is not null and not an instance of <see cref="RequestId"/>
        /// </exception>
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            RequestId other = obj as RequestId;
            if (other == null)
            {
                throw new ArgumentException(string.Format(Resources.Common_ObjectMustBeOfType, new object[] { typeof(RequestId).Name }), "obj");
            }

            return Id.CompareTo(other.Id);
        }

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Compares the two request IDs for value equality
        /// </summary>
        /// <param name="left">The left-hand request ID</param>
        /// <param name="right">The right-hand request ID</param>
        /// <returns>True if equal, false otherwise</returns>
        public static bool operator ==(RequestId left, RequestId right)
        {
            return
                object.ReferenceEquals(left, right) ||
                !object.ReferenceEquals(left, null) &&
                    !object.ReferenceEquals(right, null) &&
                    left.Id == right.Id;
        }

        /// <summary>
        /// Compares two request IDs for value inequality
        /// </summary>
        /// <param name="left">The left-hand request ID</param>
        /// <param name="right">The right-hand request ID</param>
        /// <returns>True if unequal, false otherwise</returns>
        public static bool operator !=(RequestId left, RequestId right)
        {
            return !(left == right);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying GUID that represents the request ID
        /// </summary>
        public Guid Id
        {
            get;
            private set;
        }

        #endregion
    }
}
