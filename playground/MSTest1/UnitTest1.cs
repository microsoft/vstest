// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#nullable disable

namespace MSTest1;
[ExtensionUri("uri://myadapter")]
[DefaultExecutorUri("uri://myadapter")]
public class Adapter : ITestExecutor2, ITestDiscoverer
{
    public void Cancel()
    {
        throw new System.NotImplementedException();
    }

    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        discoverySink.SendTestCase(new TestCase("abc", new Uri("uri://myadapter"), typeof(Adapter).Assembly.Location));
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {

    }

    public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {

    }

    public bool ShouldAttachToTestHost(IEnumerable<string> sources, IRunContext runContext)
    {
        return true;
    }

    public bool ShouldAttachToTestHost(IEnumerable<TestCase> tests, IRunContext runContext)
    {
        return true;
    }
}
