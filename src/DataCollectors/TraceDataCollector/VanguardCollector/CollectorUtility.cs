// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Collector
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Coverage;
/* #if !NETSTANDARD
    using Microsoft.VisualStudio.Setup.Interop;
#endif */
    using Microsoft.VisualStudio.TestTools.Diagnostics;
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

        public static string GetVSInstallPath()
        {
            string toolsPath = null;

            // TODO For netstandard find toolPath relative to dotnet.exe.
/* #if !NETSTANDARD
            try
            {
                // Use the Setup API to find the installation folder for currently running VS instance.
                var setupConfiguration = new SetupConfiguration() as ISetupConfiguration;
                if (setupConfiguration != null)
                {
                    var currentConfiguration = setupConfiguration.GetInstanceForCurrentProcess();
                    var currentInstallationPath = currentConfiguration.GetInstallationPath();
                    toolsPath = Path.Combine(currentInstallationPath, @"Common7\IDE");
                    return toolsPath;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch
            {
                // SetupConfiguration won't work in xcopy scenario.
                // So ignore all exception from it.
            }

            toolsPath = GetVSIDEPathRelativeToTraceDataCollector();
#endif */
            return toolsPath;
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

/*        /// <summary>
        /// Returns the VS Install Path relative to location of TraceDataCollector dll. 
        /// </summary>
        /// <returns>vs install path</returns>
        private static string GetVSIDEPathRelativeToTraceDataCollector()
        {
            string installDir = string.Empty;
            try
            {
// TODO: netstandard 1.5 doesn't have Assembly.GetExecutingAssembly, find alternative if required.
#if !NETSTANDARD
                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                installDir = Path.Combine(currentDirectory, "..", "..", "..");
#endif 
            }
            catch (Exception ex)
            {
                EqtTrace.Error("VS Install Dir Not found", ex.Message);
            }

            return installDir;
        }*/


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
            /*#if !NETSTANDARD
                        string toolsPath = CollectorUtility.GetVSInstallPath();

                        if (!string.IsNullOrWhiteSpace(toolsPath))
                        {
                            string path = Path.Combine(toolsPath, "..", "..");
                            
                        }
            #else*/
            return Path.GetDirectoryName(typeof(CollectorUtility).GetTypeInfo().Assembly.Location);
        }
    }
}