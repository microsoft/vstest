// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    public class RunnnerInfo
    {
        public RunnnerInfo(string runnerType, string targetFramework): this(runnerType, targetFramework, "")
        {
        }

        public RunnnerInfo(string runnerType, string tragetFramework, string inIsolation)
        {
            this.RunnerFramework = runnerType;
            this.TargetFramework = tragetFramework;
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
    }
}
