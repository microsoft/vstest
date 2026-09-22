// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;

/// <summary>
/// Indicates the test run progress
/// </summary>
internal sealed class ProgressIndicator : IProgressIndicator, IDisposable
{
    private readonly object _syncObject = new();
    private readonly ISystemTimersTimer _timer;
    private readonly string _testRunProgressString;

    private int _dotCounter;

    /// <summary>
    /// Used to output to the console
    /// </summary>
    public IOutput ConsoleOutput { get; private set; }

    /// <summary>
    /// Wrapper over the System Console class
    /// </summary>
    public IConsoleHelper ConsoleHelper { get; private set; }

    /// <summary>
    /// True is the indicator is to be displayed
    /// </summary>
    public bool IsRunning { get; private set; }

    public ProgressIndicator(IOutput output, IConsoleHelper consoleHelper, ISystemTimersTimer? timer = null)
    {
        _timer = timer ?? new SystemTimersTimer(1000);
        ConsoleOutput = output;
        ConsoleHelper = consoleHelper;
        _testRunProgressString = string.Format(CultureInfo.CurrentCulture, "{0}...", Resources.Resources.ProgressIndicatorString);

        // Subscribe exactly once for the lifetime of this instance. Start() used to
        // re-subscribe on every call, and since Pause()/Start() are invoked around every
        // logged console message, that caused the handler to be registered many times over.
        _timer.Elapsed += Timer_Elapsed;
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_syncObject)
        {
            _timer.Start();

            // Print the string based on the previous state, that is dotCounter
            // This is required for smooth transition
            ConsoleOutput.Write(_testRunProgressString.Substring(0, _testRunProgressString.Length + _dotCounter - 2), OutputLevel.Information);
            IsRunning = true;
        }
    }

    /// <summary>
    /// Get the current cursor position
    /// Clear the console starting given position
    /// Reset the cursor position back
    /// </summary>
    /// <param name="startPos">the starting position</param>
    private void Clear(int startPos)
    {
        // Defensively clamp a negative start position (can happen if Timer_Elapsed observes a
        // cursor position that hasn't caught up yet) so we never pass a negative value to
        // Console.SetCursorPosition, which would throw ArgumentOutOfRangeException.
        startPos = Math.Max(0, startPos);

        var currentLineCursor = ConsoleHelper.CursorTop;
        ConsoleHelper.SetCursorPosition(startPos, ConsoleHelper.CursorTop);
        var fillLength = Math.Max(0, ConsoleHelper.WindowWidth - startPos);
        ConsoleOutput.Write(new string(' ', fillLength), OutputLevel.Information);
        ConsoleHelper.SetCursorPosition(startPos, currentLineCursor);
    }

    /// <summary>
    /// Sets the isRunning flag to false so that indicator is not shown.
    /// </summary>
    public void Pause()
    {
        lock (_syncObject)
        {
            IsRunning = false;
            Clear(0);
        }
    }

    /// <summary>
    /// Stops the indicator and clears the current line.
    /// </summary>
    public void Stop()
    {
        lock (_syncObject)
        {
            IsRunning = false;
            _timer?.Stop();
            Clear(0);
        }
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // Run under the same lock as Start/Pause/Stop so that reading IsRunning and clearing
        // the console line are atomic with respect to state transitions triggered from other
        // threads, closing a race that could otherwise compute a stale/negative cursor position.
        lock (_syncObject)
        {
            if (IsRunning)
            {
                // If running, prints dot every second.
                ConsoleOutput.Write(".", OutputLevel.Information);
                _dotCounter = ++_dotCounter % 3;

                // When counter reaches 3, that is 3 dots have been printed
                // Clear and start printing again
                if (_dotCounter == 0)
                {
                    Clear(ConsoleHelper.CursorLeft - 3);
                }
            }
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
