// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System;
    using System.IO;
    using System.Reflection.PortableExecutable;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    public class PEReaderHelper
    {
        private static PEReaderHelper instance;

        /// <summary>
        /// Gets the PEReaderHelper instance.
        /// </summary>
        internal static PEReaderHelper Instance => instance ?? (instance = new PEReaderHelper());

        /// <summary>
        /// Determines assembly type from file.
        /// </summary>
        public AssemblyType GetAssemblyType(string filePath)
        {
            var assemblyType = AssemblyType.None;

            try
            {
                using (var assemblyStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                using (var peReader = new PEReader(assemblyStream))
                {
                    var peHeaders = peReader.PEHeaders;
                    var corHeader = peHeaders.CorHeader;
                    var corHeaderStartOffset = peHeaders.CorHeaderStartOffset;

                    assemblyType = (corHeader != null && corHeaderStartOffset >= 0) ?
                        AssemblyType.Managed :
                        AssemblyType.Native;
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("GetAssemblyTypeFromAssemblyMetadata: failed to determine assembly type: {0} for assembly: {1}", ex, filePath);
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("AssemblyMetadataProvider.GetAssemblyType: Determined assemblyType:'{0}' for source: '{1}'", assemblyType, filePath);
            }

            return assemblyType;
        }
    }
}
