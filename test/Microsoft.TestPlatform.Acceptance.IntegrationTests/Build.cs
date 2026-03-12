// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace Microsoft.TestPlatform.Acceptance.IntegrationTests;

[TestClass]
public class Build : IntegrationTestBase
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        Microsoft.TestPlatform.Acceptance.IntegrationTests.IntegrationTestBuild.BuildTestAssetsForIntegrationTests(testContext);
    }
}
