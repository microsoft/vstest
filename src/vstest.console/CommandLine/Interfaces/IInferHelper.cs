// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Defines interface for infering run configuration values.
    /// </summary>
    internal interface IInferHelper
    {
        /// <summary>
        /// Determines Framework from sources.
        /// </summary>
        Framework AutoDetectFramework(List<string> sources);


        /// <summary>
        /// Determines Architecture from sources.
        /// </summary>
        Architecture AutoDetectArchitecture(List<string> sources);
    }
}
