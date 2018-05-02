// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    using System;
    using System.IO;
    using System.Xml;
    using TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The IVangurd interface.
    /// </summary>
    internal interface IDirectoryHelper
    {
        void Delete(string directoryPath, bool recursive);

        void CreateDirectory(string directoryPath);
    }
}