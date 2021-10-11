// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#if !NETSTANDARD1_0 

using System;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces
{
    public interface IWindowsRegistryHelper
    {
        IRegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view);
    }

    public interface IRegistryKey : IDisposable
    {
        IRegistryKey OpenSubKey(string name);
        object GetValue(string name);
    }
}

#endif
