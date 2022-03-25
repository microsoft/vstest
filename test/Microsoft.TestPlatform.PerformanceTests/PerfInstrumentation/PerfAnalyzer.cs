// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.IO;

#if NETFRAMEWORK
using System;
using System.Linq;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
#endif

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests.PerfInstrumentation;

/// <summary>
/// The performance analyzer.
/// </summary>
public class PerfAnalyzer
{
    /// <summary>
    /// The etw session provider name.
    /// </summary>
    private const string EtwSessionProviderName = "TestPlatform";

#if NETFRAMEWORK
    private readonly string _perfDataFileName;
    private readonly TraceEventSession _traceEventSession;
    private readonly Dictionary<string, List<TestPlatformTask>> _testPlatformTaskMap;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="PerfAnalyzer"/> class.
    /// </summary>
    public PerfAnalyzer()
    {
#if NETFRAMEWORK
        // It is important to make the file and session name random,
        // otherwise there are a lot of errors from wmi that are "random".
        var name = $"TestPlatform{Guid.NewGuid()}";
        _perfDataFileName = $"{name}.etl";
        _testPlatformTaskMap = new Dictionary<string, List<TestPlatformTask>>();
        _traceEventSession = new TraceEventSession(name, _perfDataFileName);
#endif
    }

    /// <summary>
    /// The enable provider.
    /// </summary>
    public void EnableProvider()
    {
#if NETFRAMEWORK
        _traceEventSession.StopOnDispose = true;
        _traceEventSession.EnableProvider(EtwSessionProviderName);
#endif
    }

    /// <summary>
    /// The disable provider.
    /// </summary>
    public void DisableProvider(bool wait = false)
    {
#if NETFRAMEWORK
        Console.WriteLine($"Lost events: {_traceEventSession.EventsLost}");
        _traceEventSession.Flush();
        _traceEventSession.Dispose();
#endif
    }

    /// <summary>
    /// The analyze events data.
    /// </summary>
    public void AnalyzeEventsData()
    {
#if NETFRAMEWORK
        using var source = new ETWTraceEventSource(_perfDataFileName);
        // Open the file
        var parser = new DynamicTraceEventParser(source);
        parser.All += delegate (TraceEvent data)
        {
            try
            {
                if (data.ProviderName.Equals("TestPlatform") && !data.EventName.Equals("ManifestData"))
                {
                    Console.WriteLine("Received Event : {0}", data.ToString());
                    var key = data.ProcessID + "_" + data.ThreadID.ToString() + "_" + data.TaskName;

                    if (!_testPlatformTaskMap.ContainsKey(key))
                    {
                        var list = new List<TestPlatformTask> { CreateTestPlatformTask(data) };
                        _testPlatformTaskMap.Add(key, list);
                    }
                    else
                    {
                        if (data.Opcode == TraceEventOpcode.Start)
                        {
                            _testPlatformTaskMap[key].Add(CreateTestPlatformTask(data));
                        }
                        else
                        {
                            UpdateTask(_testPlatformTaskMap[key].Last(), data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        };
        source.Process(); // Read the file, processing the callbacks.

        source.StopProcessing();
        File.Delete(_perfDataFileName);
#endif
    }

    /// <summary>
    /// The get elapsed time by task name.
    /// </summary>
    /// <param name="taskName">
    /// The task name.
    /// </param>
    /// <returns>
    /// The <see cref="double"/>.
    /// </returns>
    public double GetElapsedTimeByTaskName(string taskName)
    {
        var timeTaken = 0.0;
#if NETFRAMEWORK
        var key = GetEventKey(taskName);

        if (key != null)
        {
            var task = _testPlatformTaskMap[key].First();
            timeTaken = task.EventStopped - task.EventStarted;
        }
#endif
        return timeTaken;
    }

    /// <summary>
    /// The get event data by task name.
    /// </summary>
    /// <param name="taskName">
    /// The task name.
    /// </param>
    /// <returns>
    /// The <see cref="IDictionary"/>.
    /// </returns>
    public IDictionary<string, string> GetEventDataByTaskName(string taskName)
    {
        IDictionary<string, string> properties = new Dictionary<string, string>();
#if NETFRAMEWORK
        var key = GetEventKey(taskName);

        if (key != null)
        {
            properties = _testPlatformTaskMap[key].First().PayLoadProperties;
        }
#endif
        return properties;
    }

    public double GetAdapterExecutionTime(string executorUri)
    {
        var timeTaken = 0.0;
#if NETFRAMEWORK
        var key = GetEventKey(Constants.AdapterExecutionTask);

        if (key != null)
        {
            var task = _testPlatformTaskMap[key].Find(t => t.PayLoadProperties["executorUri"].Equals(executorUri));
            timeTaken = task.EventStopped - task.EventStarted;
        }
#endif
        return timeTaken;
    }

    public long GetAdapterExecutedTests(string executorUri)
    {
        long totalTestsExecuted = 0;
#if NETFRAMEWORK
        var key = GetEventKey(Constants.AdapterExecutionTask);

        if (key != null)
        {
            var task = _testPlatformTaskMap[key].Find(t => t.PayLoadProperties["executorUri"].Equals(executorUri));
            _ = long.TryParse(task.PayLoadProperties["numberOfTests"], out totalTestsExecuted);
        }
#endif
        return totalTestsExecuted;
    }

#if NETFRAMEWORK

    private string GetEventKey(string taskName)
    {
        string key = null;

        key = _testPlatformTaskMap.Keys.FirstOrDefault(k => k.Split('_')[2].Equals(taskName));

        return key;
    }

    private static TestPlatformTask CreateTestPlatformTask(TraceEvent data)
    {
        var task = new TestPlatformTask(data.TaskName, data.TimeStampRelativeMSec);
        task.PayLoadProperties = GetPayloadProperties(data);
        return task;
    }

    private static void UpdateTask(TestPlatformTask task, TraceEvent data)
    {
        task.EventStopped = data.TimeStampRelativeMSec;
        var payLoadProperties = GetPayloadProperties(data);

        //Merging dictionaries look for better way
        foreach (var k in payLoadProperties.Keys)
        {
            if (!task.PayLoadProperties.ContainsKey(k))
            {
                task.PayLoadProperties.Add(k, payLoadProperties[k]);
            }
        }
    }

    private static IDictionary<string, string> GetPayloadProperties(TraceEvent data)
    {
        var payLoadProperties = new Dictionary<string, string>();

        foreach (var payLoad in data.PayloadNames)
        {
            var payLoadData = data.PayloadByName(payLoad).ToString();
            if (!payLoadProperties.ContainsKey(payLoad))
            {
                payLoadProperties.Add(payLoad, payLoadData);
            }
            else
            {
                payLoadProperties[payLoad] = payLoadData;
            }
        }

        return payLoadProperties;
    }

    internal List<KeyValuePair<string, double>> SummarizeExecution()
    {
        // We get data like this:
        // Received Event : <Event MSec = "37.4306" PID="63988" PName=        "" TID="65020" EventName="TranslationLayerInitialize/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "82.1986" PID="63988" PName=        "" TID="65020" EventName="TranslationLayerExecution/Start" ProviderName="TestPlatform" customTestHost="0" sourcesCount="1" testCasesCount="0" runSettings="&lt;RunSettings&gt;&lt;RunConfiguration&gt;&lt;/RunConfiguration&gt;&lt;/RunSettings&gt;"/>
        // Received Event : <Event MSec = "164.6160" PID="65344" PName=        "" TID="64932" EventName="VsTestConsole/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1030.9380" PID="63988" PName=        "" TID="65020" EventName="TranslationLayerInitialize/Stop" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1157.9359" PID="65344" PName=        "" TID="14304" EventName="ExecutionRequest/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1255.4776" PID="65100" PName=        "" TID="57372" EventName="TestHost/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1509.4913" PID="65100" PName=        "" TID="44896" EventName="AdapterSearch/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1879.8796" PID="65100" PName=        "" TID="44896" EventName="AdapterSearch/Stop" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1957.4240" PID="65100" PName=        "" TID="44896" EventName="Execution/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "1960.4397" PID="65100" PName=        "" TID="44896" EventName="AdapterExecution/Start" ProviderName="TestPlatform" executorUri="executor://codedwebtestadapter/v1"/>
        // Received Event : <Event MSec = "2085.8588" PID="65100" PName=        "" TID="44896" EventName="AdapterExecution/Stop" ProviderName="TestPlatform" numberOfTests="0"/>
        // Received Event : <Event MSec = "2085.8821" PID="65100" PName=        "" TID="44896" EventName="AdapterExecution/Start" ProviderName="TestPlatform" executorUri="executor://mstestadapter/v1"/>
        // Received Event : <Event MSec = "2100.0353" PID="65100" PName=        "" TID="44896" EventName="AdapterExecution/Stop" ProviderName="TestPlatform" numberOfTests="0"/>
        // Received Event : <Event MSec = "2100.0486" PID="65100" PName=        "" TID="44896" EventName="AdapterExecution/Start" ProviderName="TestPlatform" executorUri="executor://mstestadapter/v2"/>
        // Received Event : <Event MSec = "2683.8718" PID="65100" PName=        "" TID="44896" EventName="AdapterExecution/Stop" ProviderName="TestPlatform" numberOfTests="1"/>
        // Received Event : <Event MSec = "2684.0644" PID="65100" PName=        "" TID="44896" EventName="Execution/Stop" ProviderName="TestPlatform" numberOfTests="1"/>
        // Received Event : <Event MSec = "2756.0697" PID="65100" PName=        "" TID="57372" EventName="TestHost/Stop" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "2949.0084" PID="65344" PName=        "" TID="14304" EventName="ExecutionRequest/Stop" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "3116.5923" PID="63988" PName=        "" TID="65020" EventName="TranslationLayerExecution/Stop" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "3117.8503" PID="65344" PName=        "" TID="64932" EventName="VsTestConsole/Stop" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "3117.8734" PID="65344" PName=        "" TID="64932" EventName="MetricsDispose/Start" ProviderName="TestPlatform"/>
        // Received Event : <Event MSec = "3118.0411" PID="65344" PName=        "" TID="64932" EventName="MetricsDispose/Stop" ProviderName="TestPlatform"/>
        //
        // 0ms at calling EnableProvider
        // TranslationLayerInitialize/Start <- VSTestConsoleWrapper.StartSession called
        // TranslationLayerExecution/Start <-  when we get the request sent in but it can't start processing until console is up
        // VsTestConsole/Start <- console started (Executor.Execute), but is not done initializing yet
        // TranslationLayerInitialize/Stop <- we connected to console
        // ExecutionRequest/Start <- request is parsed, and ready to execute
        // TestHost/Start <- TestHost is up (in Program.main) but is not done initializing yet
        // AdapterSearch/Start <- testhost is done initializing and is looking up AdapterSearch
        // AdapterSearch/Stop <- testhost is done looking up adapters and waits for request
        // Execution/Start <- test execution starts (finally)
        // Execution/Stop <- test execution is done
        // TestHost/Stop <- testhost is done (not intersting and can be out of order)
        // ExecutionRequest/Stop <- request is done on console side, (interesting but unreliable, might not be written)
        // TranslationLayerExecution/Stop <- translation layer is done executing
        // VsTestConsole/Stop <-  vstest.console is closing up after we called stop session (not interesting, can be out of order)
        // MetricsDispose/Start <- we are about to send metrics ( not interesting )
        // MetricsDispose/Stop <- we are fully done and console exited

        var rundown = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TranslationLayerInitialize/Start"] = "TranslationLayerReadyForRequests",
            ["TranslationLayerExecution/Start"] = "TranslationLayerRequestReceived",
            ["VsTestConsole/Start"] = "VSTestConsoleProcessStarted",
            ["TranslationLayerInitialize/Stop"] = "VSTestConsoleReadyForRequests",
            ["ExecutionRequest/Start"] = "VSTestConsoleRequestReceived",
            ["TestHost/Start"] = "TestHostProcessStarted",
            ["AdapterSearch/Stop"] = "TestHostReadyForRequests",
            ["Execution/Start"] = "TestHostRequestReceived",
            // This is where tests execute. In the graph of what is happening we will exclude this.
            // As it depends on how many tests we have. And should be proportional to that.
            ["Execution/Stop"] = "TestHostRequestFinished",
            ["TranslationLayerExecution/Stop"] = "TranslationLayerRequestFinished",
            ["MetricsDispose/Stop"] = "Done",
        };

        double previous = 0;
        List<KeyValuePair<string, double>> events = new();
        TestPlatformTask lastTask =null;
        foreach (var pair in rundown)
        {
            var parts = pair.Key.Split('/');
            var key = parts[0];
            var action = parts[1];

            var realKey = GetEventKey(key);

            if (realKey == null)
            {
                Console.WriteLine($"EMPTY! : {key}");
            }
            var task = _testPlatformTaskMap[realKey].First();
            lastTask = task;

            if (action != "Start" && action != "Stop")
            {
                throw new InvalidOperationException($"Action must be either Start or Stop but was {action}");
            }
            var timestamp = action == "Start" ? task.EventStarted : task.EventStopped;

            var timeTaken = timestamp - previous;
            previous = timestamp;

            events.Add(new KeyValuePair<string, double>(pair.Value, timeTaken));
        }

        var sum = events.Select(e => e.Value).Sum();

        // We are adding up doubles, let's leave ourselves some leeway
        // for addition errors.
        var min = lastTask.EventStopped - 2;
        var max = lastTask.EventStopped + 2;
        var eq = min <= sum && sum <= max;
        if (!eq)
        {
            throw new InvalidOperationException($"Expected summary to add up to the same time as the whole task, but sum was {sum} and the whole task took {lastTask.EventStopped}.");
        }

        Console.WriteLine("Summary:");
        events.ForEach(p => Console.WriteLine($"{p.Key} : {p.Value}"));
        return events;
    }
#endif
}
