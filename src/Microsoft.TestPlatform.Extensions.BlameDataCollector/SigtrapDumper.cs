// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System.Diagnostics;

    internal class SigtrapDumper : IHangDumper
    {
        public void Dump(int processId, string outputFile, DumpTypeOption type)
        {
            Process.Start("kill", $"-s SIGTRAP {processId}");
        }
    }
}
