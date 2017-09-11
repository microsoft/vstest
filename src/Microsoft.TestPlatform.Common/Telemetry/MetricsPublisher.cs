// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class MetricsPublisher : IMetricsPublisher
    {
        private const string DirectoryPath = @"c:\temp";
        private const string FilePath = @"c:\temp\MyTest.txt";

        public void PublishMetrics(string eventName, IDictionary<string, string> metrics)
        {
            if (!File.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }

            foreach (var metric in metrics)
            {
                String[] arr =
                {
                    eventName,
                    metric.Key,
                    metric.Value
                };

                // This text is added only once to the file.
                if (!File.Exists(FilePath))
                {
                    File.WriteAllLines(FilePath, arr, Encoding.UTF8);
                }

                File.AppendAllLines(FilePath, arr, Encoding.UTF8);
            }
        }
    }
}
