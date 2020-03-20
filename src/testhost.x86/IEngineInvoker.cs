// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface contract for invoking the engine
    /// </summary>
    public interface IEngineInvoker
    {
        /// <summary>
        /// Invokes the Engine with the arguments
        /// </summary>
        /// <param name="argsDictionary">Arguments for the engine</param>
        void Invoke(IDictionary<string, string> argsDictionary);
    }
}
