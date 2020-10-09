// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class used to define the StartTestSessionPayload sent by the vstest.console translation layers into design mode.
    /// </summary>
    public class StartTestSessionPayload
    {
        /// <summary>
        /// RunSettings used for starting the test session.
        /// </summary>
        [DataMember]
        public IList<string> Sources { get; set; }

        /// <summary>
        /// RunSettings used for starting the test session.
        /// </summary>
        [DataMember]
        public string RunSettings { get; set; }

        /// <summary>
        /// Should metrics collection be enabled ?
        /// </summary>
        [DataMember]
        public bool CollectMetrics { get; set; }

        /// <summary>
        /// Is Debugging enabled
        /// </summary>
        [DataMember]
        public bool DebuggingEnabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool CustomLauncher { get; set; }
    }
}
