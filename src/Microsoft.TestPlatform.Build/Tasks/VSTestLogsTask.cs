// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using Microsoft.Build.Utilities;
    using Microsoft.TestPlatform.Build.Resources;

    public class VSTestLogsTask : Task
    {
        public string LogType
        {
            get;
            set;
        }

        public string ProjectFilePath
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if (string.Equals(LogType, "BuildStarted", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(Resources.BuildStarted);
            }
            else if (string.Equals(LogType, "BuildCompleted", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(Resources.BuildCompleted);
                Console.WriteLine();
            }
            else if (string.Equals(LogType, "NoIsTestProjectProperty", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(Resources.NoIsTestProjectProperty, ProjectFilePath);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
