// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.TestHostProvider.UnitTests.Hosting;

[TestClass]
public class TestRunnerConnectionInfoExtensionsTests
{
    [TestMethod]
    public void ToCommandLineOptionsShouldIncludePort()
    {
        var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets } };

        var options = connectionInfo.ToCommandLineOptions();

        Assert.StartsWith(options, "--port 123 --endpoint 127.0.0.0:123 --role client");
    }

    [TestMethod]
    public void ToCommandLineOptionsShouldIncludeEndpoint()
    {
        var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets } };

        var options = connectionInfo.ToCommandLineOptions();

        Assert.Contains("--endpoint 127.0.0.0:123", options);
    }

    [TestMethod]
    public void ToCommandLineOptionsShouldIncludeRole()
    {
        var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets } };

        var options = connectionInfo.ToCommandLineOptions();

        Assert.Contains("--role client", options);
    }

    [TestMethod]
    public void ToCommandLineOptionsShouldIncludeParentProcessId()
    {
        var connectionInfo = new TestRunnerConnectionInfo { RunnerProcessId = 123 };

        var options = connectionInfo.ToCommandLineOptions();

        Assert.IsGreaterThanOrEqualTo(options.IndexOf("--parentprocessid 123", StringComparison.OrdinalIgnoreCase), 0);
    }

    [TestMethod]
    public void ToCommandLineOptionsShouldNotIncludeDiagnosticsOptionIfNotEnabled()
    {
        var connectionInfo = default(TestRunnerConnectionInfo);

        var options = connectionInfo.ToCommandLineOptions();

        Assert.IsLessThan(options.IndexOf("--diag", StringComparison.OrdinalIgnoreCase), 0);
    }

    [TestMethod]
    public void ToCommandLineOptionsShouldIncludeDiagnosticsOptionIfEnabled()
    {
        var connectionInfo = new TestRunnerConnectionInfo { LogFile = "log.txt", TraceLevel = 3 };

        var options = connectionInfo.ToCommandLineOptions();

        Assert.EndsWith(options, "--diag log.txt --tracelevel 3");
    }
}
