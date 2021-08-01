// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public class VSTestTask : ToolTask, ITestTask
    {

        public ITaskItem TestFileFullPath { get; set; }
        public string VSTestSetting { get; set; }
        public ITaskItem[] VSTestTestAdapterPath { get; set; }
        public string VSTestFramework { get; set; }
        public string VSTestPlatform { get; set; }
        public string VSTestTestCaseFilter { get; set; }
        public string[] VSTestLogger { get; set; }
        public bool VSTestListTests { get; set; }
        public string VSTestDiag { get; set; }
        public string[] VSTestCLIRunSettings { get; set; }
        [Required]
        public ITaskItem VSTestConsolePath { get; set; }
        public ITaskItem VSTestResultsDirectory { get; set; }
        public string VSTestVerbosity { get; set; }
        public string[] VSTestCollect { get; set; }
        public bool VSTestBlame { get; set; }
        public bool VSTestBlameCrash { get; set; }
        public string VSTestBlameCrashDumpType { get; set; }
        public bool VSTestBlameCrashCollectAlways { get; set; }
        public bool VSTestBlameHang { get; set; }
        public string VSTestBlameHangDumpType { get; set; }
        public string VSTestBlameHangTimeout { get; set; }
        public ITaskItem VSTestTraceDataCollectorDirectoryPath { get; set; }
        public bool VSTestNoLogo { get; set; }

        protected override string ToolName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "dotnet.exe";
                else
                    return "dotnet";
            }
        }

        public VSTestTask()
        {
            this.LogStandardErrorAsError = true;
            this.StandardOutputImportance = "Normal";
        }

        protected override string GenerateCommandLineCommands()
        {
            return this.CreateCommandLineArguments();
        }

        protected override string GenerateFullPathToTool()
        {
            if (!string.IsNullOrEmpty(ToolPath))
            {
                return  Path.Combine(Path.GetDirectoryName(Path.GetFullPath(ToolPath)), ToolExe);
            }

            //TODO: https://github.com/dotnet/sdk/issues/20 Need to get the dotnet path from MSBuild?

            var dhp = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            if (!string.IsNullOrEmpty(dhp))
            {
                var path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dhp)), ToolExe);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            if (File.Exists(ToolExe))
            {
                return Path.GetFullPath(ToolExe);
            }

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var p in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(p, ToolExe);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }
    }
}
