// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A minimal process that sleeps for a configurable time.
// Used by zombie-child-process test to simulate a child process (e.g., Selenium WebDriver's
// Edge Driver) that outlives testhost while holding inherited pipe handles open.

using System.Globalization;
using System.Threading;

int sleepMs = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 30_000;
Thread.Sleep(sleepMs);
