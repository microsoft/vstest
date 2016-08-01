// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector
{
    using System;

    /// <summary>
    /// The test session start args.
    /// </summary>
    public class TestSessionStartArgs : InProcDataCollectionArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionStartArgs"/> class.
        /// </summary>
        public TestSessionStartArgs()
        {
            this.Configuration = String.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionStartArgs"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        public TestSessionStartArgs(string configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public string Configuration { get; set; }
    }
}
