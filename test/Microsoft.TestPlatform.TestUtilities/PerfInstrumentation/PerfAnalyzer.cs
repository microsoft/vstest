// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities.PerfInstrumentation;

using System.Collections.Generic;

#if NETFRAMEWORK
using Diagnostics.Tracing;
using Diagnostics.Tracing.Parsers;
using Diagnostics.Tracing.Session;
using System;
using System.Linq;
#endif

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
        _perfDataFileName = "TestPlatformEventsData.etl";
        _testPlatformTaskMap = new Dictionary<string, List<TestPlatformTask>>();
        _traceEventSession = new TraceEventSession("TestPlatformSession", _perfDataFileName);
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
    public void DisableProvider()
    {
#if NETFRAMEWORK
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
            long.TryParse(task.PayLoadProperties["numberOfTests"], out totalTestsExecuted);
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
#endif
}
