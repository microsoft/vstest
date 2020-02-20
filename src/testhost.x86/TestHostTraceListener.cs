// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
#if NETCOREAPP
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    internal class TestHostTraceListener : TraceListener
    {
        public static void Setup()
        {
            EqtTrace.Info("Setting up debug trace listener.");
            EqtTrace.Verbose("TestPlatformTraceListener.Setup: Removing listener {0}.", Trace.Listeners[0]);
            Trace.Listeners[0] = new TestHostTraceListener();
            EqtTrace.Verbose("TestPlatformTraceListener.Setup: Added test platform trace listener.");

#if NETCOREAPP2_1
            try
            {
                // workaround for netcoreapp2.1 where the trace listener api is not called when 
                // Debug.Assert fails. This method is internal, but the class is on purpose keeping the 
                // callback settable so tests can set the callback
                var field = typeof(Debug).GetField("s_ShowDialog", BindingFlags.Static | BindingFlags.NonPublic);
                var value = field.GetValue(null);
                field.SetValue(null, (Action<string, string, string, string>)ShowDialog);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("TestPlatformTraceListener.Setup: Failed to replace inner callback to ShowDialog in Debug.Assert. Calls to Debug.Assert with crash the test host process. {0}", ex);
            }
#endif
        }

        public override void Write(string message)
        {
            throw GetException(message);
        }

        public override void WriteLine(string message)
        {
            throw GetException(message);
        }

        public static void ShowDialog(string stackTrace, string message, string detailMessage, string _)
        {
            var text = !string.IsNullOrEmpty(message)
                ? !string.IsNullOrEmpty(detailMessage)
                    ? (message + Environment.NewLine + detailMessage)
                    : message
                : null;
            throw GetException(text);
        }

        private static DebugAssertException GetException(string message)
        {
            var debugTypes = new Type[] { typeof(Debug), typeof(Trace) };
            var stack = new StackTrace(true);

            var debugMethodFound = false;
            var frameCount = 0;
            MethodBase method = null;
            foreach (var f in stack.GetFrames())
            {
                var m = f.GetMethod();
                var declaringType = m?.DeclaringType;
                if (!debugMethodFound && (declaringType == typeof(Debug) || declaringType == typeof(Trace)))
                {
                    method = m;
                    debugMethodFound = true;
                }

                if (debugMethodFound)
                {
                    frameCount++;
                }
            }

            var stackTrace = string.Join(Environment.NewLine, stack.ToString().Split(Environment.NewLine).TakeLast(frameCount));
            var methodName = method != null ? $"{method.DeclaringType.Name}.{method.Name}" : "<method>";
            var wholeMessage = $"Method {methodName} failed with '{message}', and was translated to { typeof(DebugAssertException).FullName } to avoid terminating the process hosting the test.";

            return new DebugAssertException(wholeMessage, stackTrace);
        }
    }

#endif
}