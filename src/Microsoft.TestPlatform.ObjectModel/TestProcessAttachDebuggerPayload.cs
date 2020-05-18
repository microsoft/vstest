// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Runtime.Serialization;

    /// <summary>
    /// The test process info payload.
    /// </summary>
    [DataContract]
    public class TestProcessAttachDebuggerPayload
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="pid">The process id the debugger should attach to.</param>
        public TestProcessAttachDebuggerPayload(int pid)
        {
            this.ProcessID = pid;
        }

        /// <summary>
        /// The process id the debugger should attach to.
        /// </summary>
        [DataMember]
        public int ProcessID { get; set; }
    }
}
