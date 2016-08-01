// Copyright (c) Microsoft. All rights reserved.

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
