// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class identifying test execution id.
    /// Execution ID is assigned to test at run creation time and is guaranteed to be unique within that run.
    /// </summary>
    [DataContract]
    public sealed class TestExecId
    {
        private Guid execId;

        public TestExecId()
        {
            execId = Guid.NewGuid();
        }

        public TestExecId(Guid id)
        {
            execId = id;
        }

        [DataMember]
        public static TestExecId Empty { get; } = new TestExecId(Guid.Empty);

        [DataMember]
        public Guid Id
        {
            get { return execId; }
        }

        public override bool Equals(object obj)
        {
            return obj is TestExecId id && execId.Equals(id.execId);
        }

        public override int GetHashCode()
        {
            return execId.GetHashCode();
        }

        public override string ToString()
        {
            return execId.ToString("B");
        }
    }
}
