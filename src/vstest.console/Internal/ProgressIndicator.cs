// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class ProgressIndicator
    {
        // Move to resources
        const string testRunProgressString = "Test run in progress";

        static ManualResetEvent startedEvent = new ManualResetEvent(true);
        static ManualResetEvent pausedEvent = new ManualResetEvent(false);

        private Task progressIndicatorTask;
        private CancellationTokenSource stopTokenSource;

        public void Start()
        {
            if (progressIndicatorTask != null)
            {
                startedEvent.Set();
                pausedEvent.Reset();
                return;
            }
            stopTokenSource = new CancellationTokenSource();
            progressIndicatorTask = Task.Run(() => ShowProgress(stopTokenSource.Token), stopTokenSource.Token);
        }

        public void ShowProgress(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                startedEvent.WaitOne();

                Console.Write(testRunProgressString);

                int counter = 0;
                while (true)
                {
                    if (!startedEvent.WaitOne(0))
                        break;

                    Console.Write(".");
                    Thread.Sleep(1000);
                    counter++;
                    if (counter == 3)
                    {
                        counter = 0;
                        var currentLineCursor = Console.CursorTop;
                        Console.SetCursorPosition(Console.CursorLeft - 3, Console.CursorTop);
                        Console.Write("   ");
                        Console.SetCursorPosition(Console.CursorLeft - 3, Console.CursorTop);
                    }
                }

                Clear();
                pausedEvent.Set();
            }
        }

        private void Clear()
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public void Pause()
        {
            if (progressIndicatorTask != null)
            {
                startedEvent.Reset();
                pausedEvent.WaitOne();
            }
        }

        public void Stop()
        {
            if (progressIndicatorTask != null)
            {
                startedEvent.Reset();
                stopTokenSource.Cancel();
                progressIndicatorTask.Wait();
            }
        }
    }
}
