// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
#if NETFRAMEWORK
    using System;

    /// <summary>
    /// Suspends the instrumentation (for code coverage) of the modules which are loaded
    /// during this object is created and disposed
    /// exceeded.
    /// </summary>
    public class SuspendCodeCoverage : IDisposable
    {
        #region Private Variables

        private const string SuspendCodeCoverageEnvVarName = "__VANGUARD_SUSPEND_INSTRUMENT__";
        private const string SuspendCodeCoverageEnvVarTrueValue = "TRUE";

        private string prevEnvValue;

        /// <summary>
        /// Whether the object is disposed or not.
        /// </summary>
        private bool isDisposed = false;

        #endregion

        /// <summary>
        /// Constructor. Code Coverage instrumentation of the modules, which are loaded
        /// during this object is created and disposed, is disabled.
        /// </summary>
        public SuspendCodeCoverage()
        {
            this.prevEnvValue = Environment.GetEnvironmentVariable(SuspendCodeCoverageEnvVarName, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(SuspendCodeCoverageEnvVarName, SuspendCodeCoverageEnvVarTrueValue, EnvironmentVariableTarget.Process);
        }

        #region IDisposable

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes instance.
        /// </summary>
        /// <param name="disposing"> Should dispose. </param>
        internal void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    Environment.SetEnvironmentVariable(SuspendCodeCoverageEnvVarName, this.prevEnvValue, EnvironmentVariableTarget.Process);
                }

                this.isDisposed = true;
            }
        }

        #endregion IDisposable
    }

#endif
}
