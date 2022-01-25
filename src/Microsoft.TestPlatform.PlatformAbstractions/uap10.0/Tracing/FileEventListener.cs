// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class FileEventListener : EventListener
    {
        /// <summary>
        /// Storage file to be used to write logs
        /// </summary>
        private FileStream fileStream = null;

        /// <summary>
        /// StreamWriter to write logs to file
        /// </summary>
        private StreamWriter streamWriter = null;

        /// <summary>
        /// Name of the current log file
        /// </summary>
        private readonly string fileName;

        /// <summary>
        /// The format to be used by logging.
        /// </summary>
        private readonly string format = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tType: {1}\tId: {2}\tMessage: '{3}'";

        private readonly SemaphoreSlim semaphoreSlim = new(1);

        public FileEventListener(string name)
        {
            fileName = name;

            AssignLocalFile();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (streamWriter == null)
            {
                return;
            }

            var lines = new List<string>();

            var newFormatedLine = string.Format(format, DateTime.Now, eventData.Level, eventData.EventId, eventData.Payload[0]);

            Debug.WriteLine(newFormatedLine);

            lines.Add(newFormatedLine);

            WriteToFile(lines);
        }

        private void AssignLocalFile()
        {
            fileStream = new FileStream(fileName, FileMode.Append | FileMode.OpenOrCreate);
            streamWriter = new StreamWriter(fileStream)
            {
                AutoFlush = true
            };
        }

        private async void WriteToFile(IEnumerable<string> lines)
        {
            await semaphoreSlim.WaitAsync();

            await Task.Run(async () =>
            {
                try
                {
                    foreach (var line in lines)
                    {
                        await streamWriter.WriteLineAsync(line);
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });
        }
    }
}

#endif
