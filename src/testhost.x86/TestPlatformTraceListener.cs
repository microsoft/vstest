// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
#if !NET451
    internal class TestPlatformTraceListener : TraceListener
    {
        internal static void Setup()
        {
            Trace.Listeners.RemoveAt(0);
            Trace.Listeners.Add(new TestPlatformTraceListener());
        }

        public override void Write(string message)
        {
            var stack = new StackTrace(true);
            throw new DebugAssertException(GetReport(message, stack), stack);
        }

        public override void WriteLine(string message)
        {
            var stack = new StackTrace(true);
            throw new DebugAssertException(GetReport(message, stack), stack);
        }

        private string GetReport(string message, StackTrace stack)
        {
            var frames = stack.GetFrames();
            string report = null;
            MethodBase debugMethod = null;
            StackFrame frame = null;
            foreach (var f in frames)
            {
                if (debugMethod != null)
                {
                    if (f.HasMethod())
                    {
                        frame = f;
                        break;
                    }
                }

                if (f.HasMethod())
                {
                    var m = f.GetMethod();
                    if (m.DeclaringType == typeof(Debug))
                    {
                        debugMethod = m;
                    }
                }
            }

            if (frame != null)
            {
                if (frame.HasSource())
                {
                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();
                    string line = null;
                    using (var sr = new StreamReader(fileName))
                    {
                        for (int i = 1; i < lineNumber; i++)
                            sr.ReadLine();
                        line = sr.ReadLine();
                    }

                    var filteredMessage = message == "Fail:" ? null : message;

                    report += Environment.NewLine;
                    report += Environment.NewLine;

                    if (filteredMessage != null)
                    {
                        report += filteredMessage;
                    }

                    if (line != null)
                    {
                        report = string.Join(Environment.NewLine, "Debug failed at:", line.Trim());
                    }
                }
            }

            return report;
        }
    }

    internal class DebugAssertException : Exception
    {
        public DebugAssertException(string message, StackTrace stack) : base(message ?? "Debug.Assert failed.")
        {
            StackTrace = stack.ToString();
        }

        public override string StackTrace { get; }
    }

    internal class DebugFailException : Exception
    {
        public DebugFailException(string message, StackTrace stack) : base(message ?? "Debug.Fail failed.")
        {
            StackTrace = stack.ToString();
        }

        public override string StackTrace { get; }
    }
#endif
}