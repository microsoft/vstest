// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers
{
    using System;
    using ObjectModel;

    public class EnvironmentHelper
    {
        public const string VstestConnectionTimeout = "VSTEST_CONNECTION_TIMEOUT";
        public const int DefaultConnectionTimeout = 90; // seconds

        public const string VstestCloseTimeout = "VSTEST_CLOSE_TIMEOUT";
        public const int DefaultCloseTimeout = 0; // seconds

        /// <summary>
        /// Get timeout (seconds) based on environment variable VSTEST_CONNECTION_TIMEOUT.
        /// </summary>
        public static int GetConnectionTimeout()
        {
            return GetIntegerEnvironmentVariable(nameof(GetConnectionTimeout), VstestConnectionTimeout, DefaultConnectionTimeout);
        }

        /// <summary>
        /// Get timeout (seconds) based on environment variable VSTEST_CLOSE_TIMEOUT.
        /// </summary>
        public static int GetCloseTimeout()
        {
            return GetIntegerEnvironmentVariable(nameof(GetCloseTimeout), VstestCloseTimeout, DefaultCloseTimeout);
        }

        private static int GetIntegerEnvironmentVariable(string description, string environmentVatiableName, int defaultValue)
        {
            var envVarValue = Environment.GetEnvironmentVariable(environmentVatiableName);
            if (!string.IsNullOrEmpty(envVarValue) && int.TryParse(envVarValue, out int value) && value >= 0)
            {
                EqtTrace.Info("EnvironmentHelper.{0}: {1} value set to {2}.", description, defaultValue, value);
                return value;
            }

            return defaultValue;
        }
    }
}
