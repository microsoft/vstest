// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector;

using System;

using ObjectModel;

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
    public static void Main(string[] args)
    {
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
}