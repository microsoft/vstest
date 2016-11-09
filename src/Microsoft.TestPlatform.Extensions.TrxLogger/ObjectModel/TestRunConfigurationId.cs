// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;

    /// <summary>
    /// The test run configuration id.
    /// </summary>
    internal sealed class TestRunConfigurationId
    {
        private Guid id;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunConfigurationId"/> class.
        /// </summary>
        public TestRunConfigurationId()
        {
            this.id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }
    }
}
