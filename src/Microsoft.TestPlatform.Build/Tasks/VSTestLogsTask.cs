// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System;
    using Microsoft.Build.Framework;
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
                Log.LogMessage(MessageImportance.Normal, Resources.BuildStarted);
            }
            else if (string.Equals(LogType, "BuildCompleted", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogMessage(MessageImportance.Normal, Resources.BuildCompleted + Environment.NewLine);
            }
            else if (string.Equals(LogType, "NoIsTestProjectProperty", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogMessage(MessageImportance.Low, Resources.NoIsTestProjectProperty);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
