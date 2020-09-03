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

    internal class TestHostTraceListener : DefaultTraceListener
    {
        public static void Setup()
        {
            EqtTrace.Info("Setting up debug trace listener.");
            // in the majority of cases there will be only a single DefaultTraceListener in this collection
            // and we will replace that with our listener, in case there are listeners of different types we keep
            // them as is
            for (var i = 0; i < Trace.Listeners.Count; i++)
            {
                var listener = Trace.Listeners[i];
                if (listener is DefaultTraceListener)
                {
                    EqtTrace.Verbose($"TestPlatformTraceListener.Setup: Replacing listener {0} with { nameof(TestHostTraceListener) }.", Trace.Listeners[i]);
                    Trace.Listeners[i] = new TestHostTraceListener();
                }
            }

            EqtTrace.Verbose("TestPlatformTraceListener.Setup: Added test platform trace listener.");

            // this is a netcoreapp2.1 only fix, but because we always compile against netcoreapp2.1 
            // and upgrade the executable as necessary this needs to be a runtime check and not a compile time 
            // check. This call returns ".NET Core 4.6.xxx" on netcore 2.1 and older, and ".NET Core 3.1.xxx"
            // or the respective version on the newer runtimes
            if (System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core 4.6"))
            {
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
            }
        }

        public override void Fail(string message)
        {
            throw GetException(message);
        }

        public override void Fail(string message, string detailMessage)
        {
            throw GetException((message + Environment.NewLine + detailMessage));
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
#if NETCOREAPP1_0
            Exception exceptionForStack;
            try
            {
                throw new Exception();
            }
            catch (Exception e)
            {
                exceptionForStack = e;
            }

            var stack = new StackTrace(exceptionForStack, true);
#else
            var stack = new StackTrace(true);
#endif

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

#if NETCOREAPP1_0
            var stackTrace = string.Join(Environment.NewLine, stack.ToString().Replace(Environment.NewLine, "\n").Split('\n').Reverse().Take(frameCount).Reverse());
#else
            var stackTrace = string.Join(Environment.NewLine, stack.ToString().Split(Environment.NewLine).TakeLast(frameCount));
#endif
            var methodName = method != null ? $"{method.DeclaringType.Name}.{method.Name}" : "<method>";
            var wholeMessage = $"Method {methodName} failed with '{message}', and was translated to { typeof(DebugAssertException).FullName } to avoid terminating the process hosting the test.";

            return new DebugAssertException(wholeMessage, stackTrace);
        }
    }

#endif
}