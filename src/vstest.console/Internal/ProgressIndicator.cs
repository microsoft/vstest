// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using System;
    using Timer = System.Timers.Timer;

    public class ProgressIndicator
    {
        const string testRunProgressString = "Test run in progress...";
        private object syncObject = new object();
        private int dotCounter;
        private Timer timer;
        private bool isRunning;
        private DateTime startTime;

        public void Start()
        {
            lock (syncObject)
            {
                if (timer == null)
                {
                    timer = new Timer(1000);
                    timer.Elapsed += Timer_Elapsed;
                    timer.Enabled = true;
                }
                startTime = DateTime.Now;
                Console.Write(testRunProgressString.Substring(0, testRunProgressString.Length + dotCounter - 2));
                isRunning = true;
            }
        }

        private void Clear(int startPos)
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(startPos, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth - startPos));
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
                Console.Write(".");
                dotCounter = ++dotCounter % 3;
                if (dotCounter == 0)
                {
                    Clear(Console.CursorLeft - 3);
                }
            }
        }
    }
}
