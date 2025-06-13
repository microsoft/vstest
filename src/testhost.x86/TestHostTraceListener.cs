// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.TestHost;

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
                EqtTrace.Verbose($"TestPlatformTraceListener.Setup: Replacing listener {Trace.Listeners[i]} with {nameof(TestHostTraceListener)}.");
                Trace.Listeners[i] = new TestHostTraceListener();
            }
        }

        EqtTrace.Verbose("TestPlatformTraceListener.Setup: Added test platform trace listener.");
    }

    public override void Fail(string? message)
    {
        throw GetException(message);
    }

    public override void Fail(string? message, string? detailMessage)
    {
        throw GetException((message + Environment.NewLine + detailMessage));
    }

    private static DebugAssertException GetException(string? message)
    {
        var stack = new StackTrace(true);
        var frames = stack.GetFrames();
        
        // Find the first frame that represents user code (not our listener or system internals)
        MethodBase? userMethod = null;
        int userFrameIndex = -1;
        
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            var method = frame?.GetMethod();
            var declaringType = method?.DeclaringType;
            
            if (declaringType == null) continue;
            
            // Skip our own trace listener methods
            if (declaringType == typeof(TestHostTraceListener)) continue;
            
            // Skip system diagnostics internal methods
            if (declaringType.Namespace?.StartsWith("System.Diagnostics") == true) continue;
            
            // This should be user code
            userMethod = method;
            userFrameIndex = i;
            break;
        }
        
        // Build stack trace from user code onwards
        var stackTraceBuilder = new StringBuilder();
        if (userFrameIndex >= 0)
        {
            for (int i = userFrameIndex; i < frames.Length; i++)
            {
                var frame = frames[i];
                var method = frame?.GetMethod();
                var fileName = frame?.GetFileName();
                var lineNumber = frame?.GetFileLineNumber();
                
                if (method != null)
                {
                    stackTraceBuilder.Append("   at ");
                    stackTraceBuilder.Append(method.DeclaringType?.FullName);
                    stackTraceBuilder.Append(".");
                    stackTraceBuilder.Append(method.Name);
                    stackTraceBuilder.Append("()");
                    
                    if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
                    {
                        stackTraceBuilder.Append(" in ");
                        stackTraceBuilder.Append(fileName);
                        stackTraceBuilder.Append(":line ");
                        stackTraceBuilder.Append(lineNumber);
                    }
                    
                    if (i < frames.Length - 1)
                    {
                        stackTraceBuilder.AppendLine();
                    }
                }
            }
        }
        
        var methodName = userMethod != null ? $"{userMethod.DeclaringType?.Name}.{userMethod.Name}" : "<method>";
        var wholeMessage = $"Method {methodName} failed with '{message}', and was translated to {typeof(DebugAssertException).FullName} to avoid terminating the process hosting the test.";

        return new DebugAssertException(wholeMessage, stackTraceBuilder.ToString());
    }
}

#endif
