// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    public class EnvironmentHelper
    {
        /// <summary>
        /// Caluclate timeout based on environment and default value.
        /// </summary>
        /// <param name="environment">IEnvironment implementation. </param>
        /// <param name="envVar"> Environment variable. </param>
        /// <param name="defaultValue"> Default value in seconds. </param>
        /// <returns cref="int"> Return value in milliseconds. </returns>
        public static int GetConnectionTimeout(IEnvironment environment, string envVar, int defaultValue)
        {
            var increaseTimeoutByTimes = 1.0;
            environment.GetEnviromentVariable(envVar, ref increaseTimeoutByTimes);
            var timeout = increaseTimeoutByTimes * defaultValue * 1000;
            return Convert.ToInt32(Math.Round(timeout));
        }
    }
}
