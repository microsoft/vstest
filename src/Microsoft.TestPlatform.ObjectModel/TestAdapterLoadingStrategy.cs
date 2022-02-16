﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    /// <summary>
    /// Represents a loading strategy
    /// </summary>
    [Flags]
    internal enum TestAdapterLoadingStrategy
    {
        /// <summary>
        /// A strategy not defined, Test Platfrom will load adapters normally. 
        /// </summary>
        Default = 0b0000_0000_0000_0000,

        /// <summary>
        /// Test Plarform will only load adapters specified by /TestAdapterPath (or RunConfiguration.TestAdaptersPaths node). 
        /// If a specific adapter path is provided, adapter will be loaded; if a directory path is provided adapters directly in that folder will be loaded. 
        /// If no adapter path is specified, test run will fail.
        /// This will imply /InIsolation switch and force the tests to be run in an isolated process.
        /// </summary>
        Explicit = 0b0000_0000_0000_0001,

        /// <summary>
        /// Load adapters next to source. 
        /// </summary>
        NextToSource = 0b0000_0000_0000_0010,

        /// <summary>
        /// Default runtime providers inside Extensions folder will be included
        /// </summary>
        DefaultRuntimeProviders = 0b0000_0000_0000_0100,

        /// <summary>
        /// Load adapters inside Extensions folder.
        /// </summary>
        ExtensionsDirectory  = 0b0000_0000_0000_1000,

        /// <summary>
        /// Directory wide searches will be recursive, this is required to be with with NextToSource or Explicit.
        /// </summary>
        Recursive = 0b0001_0000_0000_0000,
    }
}
