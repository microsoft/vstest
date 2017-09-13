// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
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

    internal abstract class ListExtensionsArgumentProcessor : IArgumentProcessor
    {
        private Lazy<IArgumentProcessorCapabilities> metadata;
        private Lazy<IArgumentExecutor> executor;
        private Func<IArgumentExecutor> getExecutor;
        private Func<IArgumentProcessorCapabilities> getCapabilities;

        public ListExtensionsArgumentProcessor(Func<IArgumentExecutor> getExecutor, Func<IArgumentProcessorCapabilities> getCapabilities)
        {
            this.getExecutor = getExecutor;
            this.getCapabilities = getCapabilities;
        }

        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(getExecutor);
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
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(getCapabilities);
                }

                return this.metadata;
            }
        }
    }

    #region List discoverers
    /// <summary>
    /// Argument Executor for the "-lt|--ListTests|/lt|/ListTests" command line argument.
    /// </summary>
    internal class ListDiscoverersArgumentProcessor : ListExtensionsArgumentProcessor
    {
        private const string CommandName = "/ListDiscoverers";

        public ListDiscoverersArgumentProcessor()
            : base(() => new ListDiscoverersArgumentExecutor(), () => new ListExtensionsArgumentProcessorCapabilities(CommandName))
        {
        }
    }

    internal class ListDiscoverersArgumentExecutor : IArgumentExecutor
    {
        public void Initialize(string argument)
        {
        }

        public ArgumentProcessorResult Execute()
        {
            Console.WriteLine("The following Test Discovery Add-Ins are available:");
            var testPlatform = TestPlatformFactory.GetTestPlatform();
            var extensionManager = TestDiscoveryExtensionManager.Create();
            foreach (var extension in extensionManager.Discoverers)
            {
                Console.WriteLine(extension.Value.GetType().FullName);
                Console.WriteLine("\t\tDefault Executor Uri: " + extension.Metadata.DefaultExecutorUri);
                Console.WriteLine("\t\tSupported File Types: " + string.Join(", ", extension.Metadata.FileExtension));
            }

            return ArgumentProcessorResult.Success;
        }
    }
    #endregion

    #region List executors
    /// <summary>
    /// Argument Executor for the "-lt|--ListTests|/lt|/ListTests" command line argument.
    /// </summary>
    internal class ListExecutorsArgumentProcessor : ListExtensionsArgumentProcessor
    {
        private const string CommandName = "/ListExecutors";

        public ListExecutorsArgumentProcessor()
            : base(() => new ListExecutorsArgumentExecutor(), () => new ListExtensionsArgumentProcessorCapabilities(CommandName))
        {
        }
    }

    internal class ListExecutorsArgumentExecutor : IArgumentExecutor
    {
        public void Initialize(string argument)
        {
        }

        public ArgumentProcessorResult Execute()
        {
            Console.WriteLine("The following Test Discovery Add-Ins are available:");
            var testPlatform = TestPlatformFactory.GetTestPlatform();
            var extensionManager = TestExecutorExtensionManager.Create();
            foreach (var extension in extensionManager.TestExtensions)
            {
                Console.WriteLine(extension.Value.GetType().FullName);
                Console.WriteLine("\t\tUri: " + extension.Metadata.ExtensionUri);
            }

            return ArgumentProcessorResult.Success;
        }
    }
    #endregion

    #region List loggers
    /// <summary>
    /// Argument Executor for the "-lt|--ListTests|/lt|/ListTests" command line argument.
    /// </summary>
    internal class ListLoggersArgumentProcessor : ListExtensionsArgumentProcessor
    {
        private const string CommandName = "/ListLoggers";

        public ListLoggersArgumentProcessor()
            : base(() => new ListLoggersArgumentExecutor(), () => new ListExtensionsArgumentProcessorCapabilities(CommandName))
        {
        }
    }

    internal class ListLoggersArgumentExecutor : IArgumentExecutor
    {
        public void Initialize(string argument)
        {
        }

        public ArgumentProcessorResult Execute()
        {
            Console.WriteLine("The following Test Logger Add-Ins are available:");
            var testPlatform = TestPlatformFactory.GetTestPlatform();
            var extensionManager = TestLoggerExtensionManager.Create(new NullMessageLogger());
            foreach (var extension in extensionManager.TestExtensions)
            {
                Console.WriteLine(extension.Value.GetType().FullName);
                Console.WriteLine("\t\tUri: " + extension.Metadata.ExtensionUri);
                Console.WriteLine("\t\tFriendlyName: " + string.Join(", ", extension.Metadata.FriendlyName));
            }

            return ArgumentProcessorResult.Success;
        }

        private class NullMessageLogger : IMessageLogger
        {
            public void SendMessage(TestMessageLevel testMessageLevel, string message)
            {
            }
        }
    }
    #endregion

    #region List settings providers
    /// <summary>
    /// Argument Executor for the "-lt|--ListTests|/lt|/ListTests" command line argument.
    /// </summary>
    internal class ListSettingsProvidersArgumentProcessor : ListExtensionsArgumentProcessor
    {
        private const string CommandName = "/ListSettingsProviders";

        public ListSettingsProvidersArgumentProcessor()
            : base(() => new ListSettingsProvidersArgumentExecutor(), () => new ListExtensionsArgumentProcessorCapabilities(CommandName))
        {
        }
    }

    internal class ListSettingsProvidersArgumentExecutor : IArgumentExecutor
    {
        public void Initialize(string argument)
        {
        }

        public ArgumentProcessorResult Execute()
        {
            Console.WriteLine("The following Settings Providers Add-Ins are available:");
            var testPlatform = TestPlatformFactory.GetTestPlatform();
            var extensionManager = SettingsProviderExtensionManager.Create();
            foreach (var extension in extensionManager.SettingsProvidersMap.Values)
            {
                Console.WriteLine(extension.Value.GetType().FullName);
                Console.WriteLine("\t\tSettingName: " + extension.Metadata.SettingsName);
            }

            return ArgumentProcessorResult.Success;
        }

        private class NullMessageLogger : IMessageLogger
        {
            public void SendMessage(TestMessageLevel testMessageLevel, string message)
            {
            }
        }
    }
    #endregion
}