// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class StartTestRunnerCriteria
    {
        /// <summary>
        /// 
        /// </summary>
        public StartTestRunnerCriteria()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public IList<string> Sources { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string RunSettings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public ITestHostLauncher TestHostLauncher { get; set; }
    }
}
