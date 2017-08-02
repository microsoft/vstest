// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using Microsoft.Build.Utilities;
    using Microsoft.TestPlatform.Build.Resources;

    public class BuildLogTask : Task
    {
        public bool BuildStarted
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if (BuildStarted)
            {
                Console.WriteLine(Resources.BuildStarted);
            }
            else
            {
                Console.WriteLine(Resources.BuildCompleted);
                Console.WriteLine();
            }
            return true;
        }
    }
}
