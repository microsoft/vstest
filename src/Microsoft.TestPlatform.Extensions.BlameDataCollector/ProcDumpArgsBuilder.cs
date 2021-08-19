// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ProcDumpArgsBuilder : IProcDumpArgsBuilder
    {
        /// <inheritdoc />
        public string BuildTriggerBasedProcDumpArgs(int processId, string filename, IEnumerable<string> procDumpExceptionsList, bool isFullDump, bool collectAlways)
        {
            // -accepteula: Auto accept end-user license agreement
            // -e: Write a dump when the process encounters an unhandled exception. Include the 1 to create dump on first chance exceptions.
            // -g: Run as a native debugger in a managed process (no interop).
            // -t: Write a dump when the process terminates.
            // -ma: Full dump argument.
            // -f: Filter the exceptions.
            var procdumpOverride = Environment.GetEnvironmentVariable("VSTEST_DUMP_PROCDUMPARGS")?.Trim();
            StringBuilder procDumpArgument;
            if (!string.IsNullOrWhiteSpace(procdumpOverride))
            {
                procDumpArgument = new StringBuilder(procdumpOverride).Append(" ");
            }
            else
            {
                procDumpArgument = new StringBuilder($"-accepteula -e 1 -g {(collectAlways ? "-t " : string.Empty)}");

                if (isFullDump)
                {
                    procDumpArgument.Append("-ma ");
                }

                foreach (var exceptionFilter in procDumpExceptionsList)
                {
                    procDumpArgument.Append($"-f {exceptionFilter} ");
                }
            }

            procDumpArgument.Append($"{processId} {filename}.dmp");
            var argument = procDumpArgument.ToString();

            return argument;
        }

        /// <inheritdoc />
        public string BuildHangBasedProcDumpArgs(int processId, string filename, bool isFullDump)
        {
            // -accepteula: Auto accept end-user license agreement
            // -ma: Full dump argument.
            // -n: Number of dumps to capture.
            StringBuilder procDumpArgument = new StringBuilder("-accepteula -n 1");
            if (isFullDump)
            {
                procDumpArgument.Append(" -ma");
            }

            procDumpArgument.Append($" {processId} {filename}.dmp");
            var argument = procDumpArgument.ToString();

            return argument;
        }
    }
}
