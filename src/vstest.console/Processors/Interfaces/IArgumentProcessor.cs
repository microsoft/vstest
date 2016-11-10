// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;

    /// <summary>
    /// Defines interface for interacting with a command line argument processor.
    /// Items exporting this interface will be used in processing command line arguments.
    /// </summary>
    internal interface IArgumentProcessor
    {
        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        Lazy<IArgumentExecutor> Executor { get; set; }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        Lazy<IArgumentProcessorCapabilities> Metadata { get; }
    }
}
