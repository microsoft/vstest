// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

internal sealed class FileEventListener : EventListener
{
    /// <summary>
    /// Storage file to be used to write logs
    /// </summary>
    private FileStream _fileStream;

    /// <summary>
    /// StreamWriter to write logs to file
    /// </summary>
    private StreamWriter _streamWriter;

    /// <summary>
    /// Name of the current log file
    /// </summary>
    private readonly string _fileName;

    /// <summary>
    /// The format to be used by logging.
    /// </summary>
    private readonly string _format = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tType: {1}\tId: {2}\tMessage: '{3}'";

    private readonly SemaphoreSlim _semaphoreSlim = new(1);

    public FileEventListener(string name)
    {
        _fileName = name;

        AssignLocalFile();
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (_streamWriter == null)
        {
            return;
        }

        var lines = new List<string>();

        var newFormatedLine = string.Format(_format, DateTime.Now, eventData.Level, eventData.EventId, eventData.Payload[0]);

        Debug.WriteLine(newFormatedLine);

        lines.Add(newFormatedLine);

        WriteToFile(lines);
    }

    private void AssignLocalFile()
    {
        _fileStream = new FileStream(_fileName, FileMode.Append | FileMode.OpenOrCreate);
        _streamWriter = new StreamWriter(_fileStream)
        {
            AutoFlush = true
        };
    }

    private async void WriteToFile(IEnumerable<string> lines)
    {
        await _semaphoreSlim.WaitAsync();

        await Task.Run(async () =>
        {
            try
            {
                foreach (var line in lines)
                {
                    await _streamWriter.WriteLineAsync(line);
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        });
    }
}

#endif
