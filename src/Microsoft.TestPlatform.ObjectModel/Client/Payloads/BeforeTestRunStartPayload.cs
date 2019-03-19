// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class used to define the BeforeTestRunStartPayload sent by the Vstest.console to datacollector
    /// </summary>
    public class BeforeTestRunStartPayload
    {
        /// <summary>
        /// Run settings xml.
        /// </summary>
        [DataMember]
        public string SettingsXml { get; set; }

        /// <summary>
        /// List of test sources.
        /// </summary>
        [DataMember]
        public IEnumerable<string> Sources { get; set; }
    }
}
