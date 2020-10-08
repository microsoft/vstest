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
    public class TestSessionInfo : IEquatable<TestSessionInfo>
    {
        /// <summary>
        /// 
        /// </summary>
        public TestSessionInfo()
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
            return this.Equals(obj as TestSessionInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TestSessionInfo other)
        {
            return other != null && this.Id == other.Id;
        }
    }
}
