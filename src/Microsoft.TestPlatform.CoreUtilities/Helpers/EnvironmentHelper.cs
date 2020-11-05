// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers
{
    using ObjectModel;

    using System;

    public class EnvironmentHelper
    {
        public const string VstestConnectionTimeout = "VSTEST_CONNECTION_TIMEOUT";
        public const int DefaultConnectionTimeout = 90; // seconds

        /// <summary>
        /// Get timeout based on environment variable VSTEST_CONNECTION_TIMEOUT.
        /// </summary>
        public static int GetConnectionTimeout()
        {
<<<<<<< HEAD
            var envVarValue = Environment.GetEnvironmentVariable(VstestConnectionTimeout);
=======

#if NETSTANDARD1_0
            var envVarValue = string.Empty;
#else
            var envVarValue = Environment.GetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout);
#endif

>>>>>>> 2d58f0c2d69a99414e52b114d422559bbb9caa58
            if (!string.IsNullOrEmpty(envVarValue) && int.TryParse(envVarValue, out int value) && value >= 0)
            {
                EqtTrace.Info("EnvironmentHelper.GetConnectionTimeout: {0} value set to {1}.", VstestConnectionTimeout, value);
            }
            else
            {
                value = DefaultConnectionTimeout;
            }

            return value;
        }
    }
}
