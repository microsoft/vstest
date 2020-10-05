// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class Session : IEquatable<Session>, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public Session()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Guid Id { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // Should dispose the testhosts from the map and must send that info to vstest.console.
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Session other)
        {
            return other != null && this.Id == other.Id;
        }
    }
}
