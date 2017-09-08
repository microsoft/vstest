// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    public class RunnnerInfo
    {
        public RunnnerInfo(string runnerType, string targetFramework): this(runnerType, targetFramework, "")
        {
        }

        public RunnnerInfo(string runnerType, string targetFramework, string inIsolation)
        {
            this.RunnerFramework = runnerType;
            this.TargetFramework = targetFramework;
            this.InIsolationValue = inIsolation;
        }
        /// <summary>
        /// Gets the target framework.
        /// </summary>
        public string TargetFramework
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the inIsolation.
        /// Supported values = <c>/InIsolation</c>.
        /// </summary>
        public string InIsolationValue
        {
            get; set;
        }

        /// <summary>
        /// Gets the application type.
        /// </summary>
        public string RunnerFramework
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Join(",", new[] { "RunnerFramework = " + RunnerFramework, "TargetFramework = " + TargetFramework, string.IsNullOrEmpty(InIsolationValue) ? "InProcess" : "InIsolation" });
        }
    }
}
