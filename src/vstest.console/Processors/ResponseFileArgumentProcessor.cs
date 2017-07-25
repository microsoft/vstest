// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    ///  An argument processor that allows the user to specify additional arguments from a response file.
    ///  for test run.
    /// </summary>
    internal class ResponseFileArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the OutputArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "@";

        #endregion

        private Lazy<IArgumentProcessorCapabilities> metadata;

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ResponseFileArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        /// <remarks>
        /// As this manipulates the command line arguments themselves, this has no executor.
        /// </remarks>
        public Lazy<IArgumentExecutor> Executor { get; set; }
    }

    internal class ResponseFileArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => ResponseFileArgumentProcessor.CommandName;

        public override bool AllowMultiple => true;

        public override bool IsAction => false;

        public override bool IsSpecialCommand => true;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => CommandLineResources.ResponseFileArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.ResponseFileArgumentProcessorHelpPriority;
    }
}
