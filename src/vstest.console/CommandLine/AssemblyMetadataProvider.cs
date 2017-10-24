// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Runtime.Versioning;
    using System.Text;
    using ObjectModel;

    internal class AssemblyMetadataProvider : IAssemblyMetadataProvider
    {
        private static AssemblyMetadataProvider instance;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        internal static AssemblyMetadataProvider Instance => instance ?? (instance = new AssemblyMetadataProvider());

        /// <inheritdoc />
        public FrameworkName GetFrameWork(string filePath)
        {
            FrameworkName frameworkName = new FrameworkName(Framework.DefaultFramework.Name);
            try
            {
                using (var assemblyStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    frameworkName = GetFrameworkNameFromAssemblyMetadata(assemblyStream);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("GetFrameWorkFromMetadata: failed to determine TargetFrameworkVersion: {0} for assembly: {1}", ex, filePath);
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("AssemblyMetadataProvider.GetFrameWork: Determined framework:'{0}' for source: '{1}'", frameworkName, filePath);
            }

            return frameworkName;
        }

        /// <inheritdoc />
        public Architecture GetArchitecture(string assemblyPath)
        {
            Architecture archType = Architecture.AnyCPU;
            try
            {
                // AssemblyName won't load the assembly into current domain.
                var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                archType = MapToArchitecture(assemblyName.ProcessorArchitecture);
            }
            catch (Exception ex)
            {
                // AssemblyName will thorw Exception if assembly contains native code or no manifest.

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("AssemblyMetadataProvider.GetArchitecture: Failed get ProcessorArchitecture using AssemblyName API with exception: {0}", ex);
                }

                try
                {
                    archType = GetArchitectureForSource(assemblyPath);
                }
                catch (Exception e)
                {
                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("AssemblyMetadataProvider.GetArchitecture: Failed to determine Assembly Architecture with exception: {0}", e);
                    }
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("AssemblyMetadataProvider.GetArchitecture: Determined architecture:{0} info for assembly: {1}", archType,
                    assemblyPath);
            }

            return archType;
        }

        private static FrameworkName GetFrameworkNameFromAssemblyMetadata(FileStream assemblyStream)
        {
            FrameworkName frameworkName = new FrameworkName(Framework.DefaultFramework.Name);
            using (var peReader = new PEReader(assemblyStream))
            {
                var metadataReader = peReader.GetMetadataReader();

                foreach (var customAttributeHandle in metadataReader.CustomAttributes)
                {
                    var attr = metadataReader.GetCustomAttribute(customAttributeHandle);
                    var result = Encoding.UTF8.GetString(metadataReader.GetBlobBytes(attr.Value));
                    if (result.Contains(".NET") && result.Contains(",Version="))
                    {
                        var fxStartIndex = result.IndexOf(".NET", StringComparison.Ordinal);
                        var fxEndIndex = result.IndexOf("\u0001", fxStartIndex, StringComparison.Ordinal);
                        if (fxStartIndex > -1 && fxEndIndex > fxStartIndex)
                        {
                            // Using -3 because custom attribute values seperated by unicode characters.
                            result = result.Substring(fxStartIndex, fxEndIndex - 3);
                            frameworkName = new FrameworkName(result);
                            break;
                        }
                    }
                }
            }

            return frameworkName;
        }

        private Architecture MapToArchitecture(ProcessorArchitecture processorArchitecture)
        {
            Architecture arch = Architecture.AnyCPU;
            // Mapping to Architecture based on https://msdn.microsoft.com/en-us/library/system.reflection.processorarchitecture(v=vs.110).aspx

            if (processorArchitecture.Equals(ProcessorArchitecture.Amd64)
                || processorArchitecture.Equals(ProcessorArchitecture.IA64))
            {
                arch = Architecture.X64;
            }
            else if (processorArchitecture.Equals(ProcessorArchitecture.X86))
            {
                arch = Architecture.X86;
            }else if (processorArchitecture.Equals(ProcessorArchitecture.MSIL))
            {
                arch = Architecture.AnyCPU;
            }else if (processorArchitecture.Equals(ProcessorArchitecture.Arm))
            {
                arch = Architecture.ARM;
            }
            else
            {
                EqtTrace.Info("Unable to map to Architecture, using platform: {0}", arch);
            }

            return arch;
        }

        public Architecture GetArchitectureForSource(string imagePath)
        {
            // For details refer to below code available on MSDN.
            //https://code.msdn.microsoft.com/windowsapps/CSCheckExeType-aab06100#content

            var archType = Architecture.AnyCPU;
            ushort machine = 0;

            uint peHeader;
            const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;
            const int IMAGE_FILE_MACHINE_IA64 = 0x200;
            const int IMAGE_FILE_MACHINE_I386 = 0x14c;
            const int IMAGE_FILE_MACHINE_ARM = 0x01c0; // ARM Little-Endian
            const int IMAGE_FILE_MACHINE_THUMB = 0x01c2; // ARM Thumb/Thumb-2 Little-Endian
            const int IMAGE_FILE_MACHINE_ARMNT = 0x01c4; // ARM Thumb-2 Little-Endian


            try
            {
                //get the input stream
                using (Stream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    var validImage = true;

                    //PE Header starts @ 0x3C (60). Its a 4 byte header.
                    fs.Position = 0x3C;
                    peHeader = reader.ReadUInt32();

                    // Check if the offset is invalid
                    if (peHeader > fs.Length - 5)
                    {
                        validImage = false;
                    }

                    if (validImage)
                    {
                        //Moving to PE Header start location...
                        fs.Position = peHeader;

                        var signature = reader.ReadUInt32(); //peHeaderSignature
                        // 0x00004550 is the letters "PE" followed by two terminating zeroes.
                        if (signature != 0x00004550)
                        {
                            validImage = false;
                        }

                        if (validImage)
                        {
                            //Read the image file header header.
                            machine = reader.ReadUInt16();
                            reader.ReadUInt16(); //NumberOfSections
                            reader.ReadUInt32(); //TimeDateStamp
                            reader.ReadUInt32(); //PointerToSymbolTable
                            reader.ReadUInt32(); //NumberOfSymbols
                            reader.ReadUInt16(); //SizeOfOptionalHeader
                            reader.ReadUInt16(); //Characteristics

                            // magic number.32bit or 64bit assembly.
                            var magic = reader.ReadUInt16();
                            if (magic != 0x010B && magic != 0x020B)
                            {
                                validImage = false;
                            }
                        }

                        if (validImage)
                        {
                            switch (machine)
                            {
                                case IMAGE_FILE_MACHINE_I386:
                                    archType = Architecture.X86;
                                    break;

                                case IMAGE_FILE_MACHINE_AMD64:
                                case IMAGE_FILE_MACHINE_IA64:
                                    archType = Architecture.X64;
                                    break;

                                case IMAGE_FILE_MACHINE_ARM:
                                case IMAGE_FILE_MACHINE_THUMB:
                                case IMAGE_FILE_MACHINE_ARMNT:
                                    archType = Architecture.ARM;
                                    break;
                            }
                        }
                        else
                        {
                            EqtTrace.Info(
                                "GetArchitectureForSource: Source path {0} is not a valid image path. Returning default proc arch type: {1}.",
                                imagePath, archType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Ignore all exception
                EqtTrace.Info(
                    "GetArchitectureForSource: Returning default:{0}. Unhandled exception: {1}.",
                    archType, ex.Message);
            }

            return archType;
        }
    }
}
