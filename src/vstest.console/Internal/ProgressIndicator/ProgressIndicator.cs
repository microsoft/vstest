// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System;
    using Timer = System.Timers.Timer;

    public class ProgressIndicator : IProgressIndicator
    {
        const string testRunProgressString = "Test run in progress...";
        private object syncObject = new object();
        private int dotCounter;
        private Timer timer;
        private bool isRunning;
        private DateTime startTime;

        public IOutput ConsoleOutput { get; private set; }

        public ProgressIndicator(IOutput output)
        {
            ConsoleOutput = output;
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
                ConsoleOutput.Write(testRunProgressString.Substring(0, testRunProgressString.Length + dotCounter - 2), OutputLevel.Information);
                isRunning = true;
            }
        }

        private void Clear(int startPos)
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(startPos, Console.CursorTop);
            ConsoleOutput.Write(new string(' ', Console.WindowWidth - startPos), OutputLevel.Information);
            Console.SetCursorPosition(startPos, currentLineCursor);
        }

        public void Pause()
        {
            lock (syncObject)
            {
                isRunning = false;
                Clear(0);
            }
        }

        public void Stop()
        {
            lock (syncObject)
            {
                isRunning = false;
                timer?.Stop();
                Clear(0);
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isRunning)
            {
                ConsoleOutput.Write(".", OutputLevel.Information);
                dotCounter = ++dotCounter % 3;
                if (dotCounter == 0)
                {
                    Clear(Console.CursorLeft - 3);
                }
            }
        }
    }
}
