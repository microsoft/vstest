// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Runtime.Serialization;

    [DataContract]
    public class TestProcessAttachDebuggerPayload
    {
        public TestProcessAttachDebuggerPayload(int pid)
        {
            this.ProcessID = pid;
        }

        [DataMember]
        public int ProcessID { get; set; }
    }
}
