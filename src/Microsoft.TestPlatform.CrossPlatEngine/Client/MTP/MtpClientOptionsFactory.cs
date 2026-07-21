// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.Testing.Platform.ServerMode.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// vstest-side glue for the source-only Microsoft.Testing.Platform (MTP) server client: builds the
/// <see cref="MtpServerClientOptions"/> that identify vstest to the MTP application, bridge the client's
/// own diagnostics to <see cref="EqtTrace"/>, and map the server's <c>client/log</c> levels onto vstest's
/// <see cref="TestMessageLevel"/>.
/// </summary>
internal static class MtpClientOptionsFactory
{
    /// <summary>
    /// Builds the options used to launch an MTP application: vstest's client identity, a single
    /// discover-or-run session (not stateful), the vstest connection timeout, the EqtTrace diagnostics
    /// bridge, and any environment variables to inject into the launched process.
    /// </summary>
    public static MtpServerClientOptions CreateOptions(IDictionary<string, string?>? environmentVariables = null)
    {
        var options = new MtpServerClientOptions
        {
            ClientName = "vstest",
            ClientVersion = "1.0.0",
            DebuggerProvider = false,
            IsStateful = false,
            ConnectionTimeout = GetConnectionTimeout(),
            Logger = new DelegateMtpClientLogger(Trace),
        };

        if (environmentVariables is not null)
        {
            foreach (KeyValuePair<string, string?> variable in environmentVariables)
            {
                options.EnvironmentVariables[variable.Key] = variable.Value;
            }
        }

        return options;
    }

    /// <summary>
    /// Maps a server <c>client/log</c> level string onto the vstest <see cref="TestMessageLevel"/>.
    /// </summary>
    public static TestMessageLevel MapServerLogLevel(string level)
        => level switch
        {
            "Error" or "Critical" => TestMessageLevel.Error,
            "Warning" => TestMessageLevel.Warning,
            _ => TestMessageLevel.Informational,
        };

    private static TimeSpan GetConnectionTimeout()
    {
        // Reuse vstest's connection timeout knob so users can extend it in slow environments.
        string? value = Environment.GetEnvironmentVariable("VSTEST_CONNECTION_TIMEOUT");
        return !string.IsNullOrEmpty(value) && int.TryParse(value, out int seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.FromSeconds(90);
    }

    private static void Trace(MtpClientLogLevel level, string message)
    {
        switch (level)
        {
            case MtpClientLogLevel.Error:
                EqtTrace.Error(message);
                break;

            case MtpClientLogLevel.Warning:
                EqtTrace.Warning(message);
                break;

            case MtpClientLogLevel.Information:
                EqtTrace.Info(message);
                break;

            default:
                EqtTrace.Verbose(message);
                break;
        }
    }
}
