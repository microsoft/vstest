// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using Microsoft.Diagnostics.NETCore.Client;

    internal class NetClientDumper : IHangDumper
    {
        public void Dump(int processId, string outputFile, DumpTypeOption type)
        {
            var client = new DiagnosticsClient(processId);
            client.WriteDump(type == DumpTypeOption.Full ? DumpType.Full : DumpType.Normal, outputFile);
        }
    }
}
