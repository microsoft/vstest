// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities.PerfInstrumentation
{
    using System.Collections.Generic;

    /// <summary>
    /// The test platform task.
    /// </summary>
    public class TestPlatformTask
    {
        public string TaskName { get; set; }

        public double EventStarted { get; set; }

        public double EventStopped { get; set; }

        public IDictionary<string, string> PayLoadProperties { get; set; }

        public TestPlatformTask(string taskName, double eventStarted)
        {
            this.EventStarted = eventStarted;
            this.TaskName = taskName;
            this.PayLoadProperties = new Dictionary<string, string>();
        }
    }
}
