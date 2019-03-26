// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Indicates the test run progress
    /// </summary>
    internal class ProgressIndicator : IProgressIndicator
    {
        private object syncObject = new object();
        private int dotCounter;
        private Timer timer;
        private string testRunProgressString;

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

        public ProgressIndicator(IOutput output, IConsoleHelper consoleHelper)
        {
            this.ConsoleOutput = output;
            this.ConsoleHelper = consoleHelper;
            this.testRunProgressString = string.Format(CultureInfo.CurrentCulture, "{0}...", Resources.Resources.ProgressIndicatorString);
        }

        /// <inheritdoc />
        public void Start()
        {
            lock (syncObject)
            {
                if (timer == null)
                {
                    this.timer = new Timer(1000);
                    this.timer.Elapsed += Timer_Elapsed;
                    this.timer.Start();
                }

                // Print the string based on the previous state, that is dotCounter
                // This is required for smooth transition
                this.ConsoleOutput.Write(testRunProgressString.Substring(0, testRunProgressString.Length + dotCounter - 2), OutputLevel.Information);
                this.IsRunning = true;
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
            this.ConsoleOutput.Write(new string(' ', this.ConsoleHelper.WindowWidth - startPos), OutputLevel.Information);
            this.ConsoleHelper.SetCursorPosition(startPos, currentLineCursor);
        }

        /// <summary>
        /// Sets the isRunning flag to false so that indicator is not shown.
        /// </summary>
        public void Pause()
        {
            lock (syncObject)
            {
                this.IsRunning = false;
                this.Clear(0);
            }
        }

        /// <summary>
        /// Stops the indicator and clears the current line.
        /// </summary>
        public void Stop()
        {
            lock (syncObject)
            {
                this.IsRunning = false;
                this.timer?.Stop();
                this.Clear(0);
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsRunning)
            {
                // If running, prints dot every second.
                this.ConsoleOutput.Write(".", OutputLevel.Information);
                dotCounter = ++dotCounter % 3;

                // When counter reaches 3, that is 3 dots have been printed
                // Clear and start printing again
                if (dotCounter == 0)
                {
                    this.Clear(this.ConsoleHelper.CursorLeft - 3);
                }
            }
        }
    }
}
