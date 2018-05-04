// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    using System.Xml;
    using Collector;

    /// <summary>
    /// The CollectorUtility interface.
    /// </summary>
    internal interface ICollectorUtility
    {
        /// <summary>
        /// Get path to vanguard.exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        string GetVanguardPath();

        /// <summary>
        /// Get path to vanguard.exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        string GetVanguardDirectory();

        void RemoveChildNodeAndReturnValue(ref XmlElement configurationElement, string elementName, out string elementValue);

        string GetDotnetHostFullPath();

        CollectorUtility.MachineType GetMachineType(string exePath);
    }
}