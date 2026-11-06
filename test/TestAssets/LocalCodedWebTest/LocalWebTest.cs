// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.WebTesting;

public sealed class LocalWebTest : WebTest
{
    public LocalWebTest()
    {
        this.Proxy = string.Empty;
    }

    public override IEnumerator<WebTestRequest> GetRequestEnumerator()
    {
        var url = Environment.GetEnvironmentVariable("VSTEST_WEBTEST_URL");
        if (string.IsNullOrEmpty(url) || !new Uri(url).IsLoopback)
        {
            throw new InvalidOperationException("The acceptance test must supply its loopback HTTP server.");
        }

        yield return new WebTestRequest(url) { ExpectedHttpStatusCode = 200 };
    }
}
