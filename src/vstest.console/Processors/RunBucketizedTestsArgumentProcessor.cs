// <copyright file="RunBucketizedTestsArgumentProcessor.cs" company="Microsoft Corporation">
// Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System;


    internal class RunBucketizedTestsArgumentProcessor : IArgumentProcessor
    {
        public const string CommandName = "/Bucketize";

        public Lazy<IArgumentProcessorCapabilities> Metadata { get; } =
            new Lazy<IArgumentProcessorCapabilities>(() => new RunBucketizedTestsrgumentProcessorCapabilities());

        public Lazy<IArgumentExecutor> Executor { get; set; } =
            new Lazy<IArgumentExecutor>(() => new RunSpecificTestsArgumentExecutor(
                CommandLineOptions.Instance,
                RunSettingsManager.Instance,
                TestRequestManager.Instance,
                ConsoleOutput.Instance,
                enableBucketization: true));
    }

    internal class RunBucketizedTestsrgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => RunBucketizedTestsArgumentProcessor.CommandName;

        public override bool IsAction => true;

        public override bool AllowMultiple => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;
    }
}
