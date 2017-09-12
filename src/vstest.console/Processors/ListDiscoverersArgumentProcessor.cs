// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using System;

    internal class ListExtensionsArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        private string commandName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListExtensionsArgumentProcessorCapabilities"/> class.
        /// </summary>
        /// <param name="commandName">Name of the command</param>
        public ListExtensionsArgumentProcessorCapabilities(string commandName)
        {
            this.commandName = commandName;
        }

        public override string CommandName => this.commandName;

        /// <inheritdoc />
        public override bool AllowMultiple => false;

        /// <inheritdoc />
        public override bool IsAction => true;
    }

    internal abstract class ListExtensionsArgumentExecutor
    {
    }

    #region List discoverers
    /// <summary>
    /// Argument Executor for the "-lt|--ListTests|/lt|/ListTests" command line argument.
    /// </summary>
    internal class ListDiscoverersArgumentProcessor : IArgumentProcessor
    {
        private const string CommandName = "/ListDiscoverers";
        private Lazy<IArgumentProcessorCapabilities> metadata;
        private Lazy<IArgumentExecutor> executor;

        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(() => new ListDiscoverersArgumentExecutor());
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ListExtensionsArgumentProcessorCapabilities(CommandName));
                }

                return this.metadata;
            }
        }
    }

    internal class ListDiscoverersArgumentExecutor : ListExtensionsArgumentExecutor, IArgumentExecutor
    {
        public void Initialize(string argument)
        {
        }

        public ArgumentProcessorResult Execute()
        {
            Console.WriteLine("Hello /listDiscoverers mod");
            var testPlatform = TestPlatformFactory.GetTestPlatform();
            var extensionManager = TestDiscoveryExtensionManager.Create();
            foreach (var extension in extensionManager.UnfilteredDiscoverers)
            {
                foreach (var data in extension.Metadata)
                {
                    Console.WriteLine("Pairs: {0}, {1}", data.Key, data.Value);
                }
            }

            return ArgumentProcessorResult.Success;
        }
    }
    #endregion
}