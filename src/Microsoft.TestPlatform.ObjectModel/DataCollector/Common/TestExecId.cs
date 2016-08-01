// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    
    /// <summary>
    /// Class identifying test execution id.
    /// Execution ID is assigned to test at run creation time and is guaranteed to be unique within that run.
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class TestExecId
    {
        private Guid execId;

        private static TestExecId empty = new TestExecId(Guid.Empty);

        public TestExecId()
        {
            execId = Guid.NewGuid();
        }

        public TestExecId(Guid id)
        {
            execId = id;
        }

        public static TestExecId Empty
        {
            get { return empty; }
        }

        public Guid Id
        {
            get { return execId; }
        }

        public override bool Equals(object obj)
        {
            TestExecId id = obj as TestExecId;

            if (id == null)
            {
                return false;
            }

            return execId.Equals(id.execId);
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
