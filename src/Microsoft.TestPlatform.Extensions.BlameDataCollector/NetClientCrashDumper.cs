// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    internal class NetClientCrashDumper : ICrashDumper
    {
        private string outputDirectory;
        private IFileHelper fileHelper;

        public NetClientCrashDumper(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        public void AttachToTargetProcess(int processId, string outputDirectory, DumpTypeOption dumpType, bool collectAlways)
        {
            // we don't need to do anything directly here, we setup the env variables
            // in the dumper configuration, including the path
            this.outputDirectory = outputDirectory;
        }

        public void DetachFromTargetProcess(int processId)
        {
            // here we might consider renaming the files to have timestamp
        }

        public IEnumerable<string> GetDumpFiles(bool processCrashed)
        {
            return this.fileHelper.DirectoryExists(this.outputDirectory)
               ? this.fileHelper.EnumerateFiles(this.outputDirectory, SearchOption.AllDirectories, new[] { ".dmp" })
               : new List<string>();
        }

        public void WaitForDumpToFinish()
        {
        }
    }
}
