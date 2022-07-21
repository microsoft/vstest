// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.DataCollector;

/// <summary>
/// The program.
/// </summary>
public class Program
{
    /// <summary>
    /// The main.
    /// </summary>
    /// <param name="args">
    /// The args.
    /// </param>
    public static void Main(string[]? args)
    {
        System.Diagnostics.Debug.Assert(false);
        SendTelemetry();
        try
        {
            new DataCollectorMain().Run(args);
        }
        catch (Exception ex)
        {
            EqtTrace.Error("Program.Main: Error occurred during initialization of Datacollector : {0}", ex);
            throw;
        }
        finally
        {
            EqtTrace.Info("Program.Main: exiting datacollector process.");
        }
    }

    private static void SendTelemetry()
    {
        try
        {
            TelemetryService.DefaultSession.IsOptedIn = true;
            TelemetryService.DefaultSession.UseVsIsOptedIn();
            TelemetryService.DefaultSession.Start();
            TelemetryEvent coloringEvent = new("vs/codecoverage/faisal/main");
            coloringEvent.Properties["vs.codecoverage.faisal.collectorproperty"] = false;
            TelemetryService.DefaultSession.PostEvent(coloringEvent);
            //if (networkAsync)
            //{
            //    var cts = new CancellationTokenSource(60000);
            //    await TelemetryService.DefaultSession.DisposeToNetworkAsync(cts.Token).ConfigureAwait(false);
            //}
            //else
            {
                TelemetryService.DefaultSession.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
