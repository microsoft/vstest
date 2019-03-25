// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Timer = System.Timers.Timer;

    internal class ProgressIndicator : IProgressIndicator
    {
        private object syncObject = new object();
        private int dotCounter;
        private Timer timer;
        private DateTime startTime;
        private string testRunProgressString;

        public IOutput ConsoleOutput { get; private set; }

        public IConsoleHelper ConsoleHelper { get; private set; }

        public bool IsRunning { get; private set; }

        public ProgressIndicator(IOutput output, IConsoleHelper consoleHelper)
        {
            ConsoleOutput = output;
            ConsoleHelper = consoleHelper;
            testRunProgressString = Resources.Resources.ProgressIndicatorString;
        }

        public void Start()
        {
            lock (syncObject)
            {
                if (timer == null)
                {
                    timer = new Timer(1000);
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();
                }
                startTime = DateTime.Now;

                // Print the string based on the previous state, that is dotCounter
                // This is required for smooth transition
                ConsoleOutput.Write(testRunProgressString.Substring(0, testRunProgressString.Length + dotCounter - 2), OutputLevel.Information);
                IsRunning = true;
            }
        }

        /// <summary>
        // Get the current cursor position
        // Clear the console starting given position
        // Reset the cursor position back
        /// </summary>
        /// <param name="startPos">the starting position</param>
        private void Clear(int startPos)
        {
            var currentLineCursor = this.ConsoleHelper.CursorTop;
            this.ConsoleHelper.SetCursorPosition(startPos, this.ConsoleHelper.CursorTop);
            ConsoleOutput.Write(new string(' ', this.ConsoleHelper.WindowWidth - startPos), OutputLevel.Information);
            this.ConsoleHelper.SetCursorPosition(startPos, currentLineCursor);
        }

        /// <summary>
        /// Sets the isRunning flag to false so that indicator is not shown.
        /// </summary>
        public void Pause()
        {
            lock (syncObject)
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
            lock (syncObject)
            {
                IsRunning = false;
                timer?.Stop();
                Clear(0);
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsRunning)
            {
                // If running, prints dot every second.
                ConsoleOutput.Write(".", OutputLevel.Information);
                dotCounter = ++dotCounter % 3;

                // When counter reaches 3, that is 3 dots have been printed
                // Clear and start printing again
                if (dotCounter == 0)
                {
                    Clear(this.ConsoleHelper.CursorLeft - 3);
                }
            }
        }
    }
}
