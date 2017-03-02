// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging
{
    using System.Collections.Generic;
    public class LoggerInfo
    {
        public string arument;
        public string loggerIdentifier;
        public Dictionary<string, string> parameters = new Dictionary<string, string>();

        public LoggerInfo(string arument, string loggerIdentifier, Dictionary<string, string> parameters)
        {
            this.arument = arument;
            this.loggerIdentifier = loggerIdentifier;
            this.parameters = parameters;
        }
    }
}
