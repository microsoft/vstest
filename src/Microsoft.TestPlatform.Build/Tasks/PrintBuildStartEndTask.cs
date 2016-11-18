// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System.Collections.Generic;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.IO;

    public class PrintBuildStartEndTask : Task
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
                Console.WriteLine("Build started, please wait...");
            }
            else
            {
                Console.WriteLine("Build completed.");
                Console.WriteLine();
            }
            return true;
        }
    }
}
