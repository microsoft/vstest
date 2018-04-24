// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Collector
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Coverage;
    using TraceDataCollector.Resources;

    public static class CollectorUtility
    {
        /// <summary>
        /// Vanguard executable path (relative to VS install path)
        /// </summary>
        private const string VanguardPath = @"CodeCoverage.exe";

        public enum MachineType
        {
            /// <summary>
            /// The native.
            /// </summary>
            Native = 0,

            /// <summary>
            /// The i 386.
            /// </summary>
            I386 = 0x014c,

            /// <summary>
            /// The itanium.
            /// </summary>
            Itanium = 0x0200,

            /// <summary>
            /// The x 64.
            /// </summary>
            x64 = 0x8664
        }

        public static void RemoveChildNodeAndReturnValue(ref XmlElement owner, string elementName,
            out string elementValue)
        {
            var node = owner.SelectSingleNode(elementName);
            elementValue = string.Empty;

            if (node != null)
            {
                elementValue = node.InnerText;
                owner.RemoveChild(node);
            }
        }

        public static MachineType GetMachineType(string fileName)
        {
            const int PE_POINTER_OFFSET = 60;
            const int MACHINE_OFFSET = 4;
            byte[] data = new byte[4096];
            using (Stream s = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                s.Read(data, 0, 4096);
            }

            // dos header is 64 bytes, last element, long (4 bytes) is the address of the PE header
            int PE_HEADER_ADDR = BitConverter.ToInt32(data, PE_POINTER_OFFSET);
            int machineUint = BitConverter.ToUInt16(data, PE_HEADER_ADDR + MACHINE_OFFSET);
            return (MachineType) machineUint;
        }

        public static string GetDotnetHostFullPath()
        {
            char separator = ';';
            var dotnetExeName = "dotnet.exe";

            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(separator))
            {
                string exeFullPath = Path.Combine(path.Trim(), dotnetExeName);
                if (File.Exists(exeFullPath))
                {
                    return exeFullPath;
                }
            }

            string errorMessage = string.Format("NoDotnetExeFound", dotnetExeName);

            throw new FileNotFoundException(errorMessage);
        }

        /// <summary>
        /// Get path to vanguard.exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        public static string GetVanguardPath()
        {
            var vanguardPath = Path.Combine(CollectorUtility.GetVanguardDirectory(), VanguardPath);
            if (!File.Exists(vanguardPath))
            {
                throw new VanguardException(Resources.ErrorNoVanguard);
            }
            return vanguardPath;
        }

        /// <summary>
        /// Get path to vanguard.exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        public static string GetVanguardDirectory()
        {
            return Path.GetDirectoryName(typeof(CollectorUtility).GetTypeInfo().Assembly.Location);
        }
    }
}