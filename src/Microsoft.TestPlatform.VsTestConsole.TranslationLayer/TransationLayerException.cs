// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class TransationLayerException : Exception
    {
        public TransationLayerException(string message)
            : base(message)
        {
        }

        public TransationLayerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
