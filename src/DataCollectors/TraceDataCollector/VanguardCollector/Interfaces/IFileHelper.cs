// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// The IVangurd interface.
    /// </summary>
    internal interface IFileHelper
    {
        bool Exists(string filePath);
    }
}