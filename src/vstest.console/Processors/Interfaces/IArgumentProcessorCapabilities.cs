// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    /// <summary>
    /// Represents capabilities for an Argument Executor
    /// </summary>
    internal interface IArgumentProcessorCapabilities
    {
        /// <summary>
        /// The short name of the command the ArgumentProcessor handles.  For example "/t".
        /// </summary>
        string ShortCommandName { get; }

        /// <summary>
        /// The long name of the command the ArgumentProcessor handles.  For example "/tests".
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Indicates if multiple of of the command are allowed.
        /// </summary>
        bool AllowMultiple { get; }

        /// <summary>
        /// Indicates if the command is a special command.  Special commands can not
        /// be specified directly on the command line (like run tests).
        /// </summary>
        bool IsSpecialCommand { get; }

        /// <summary>
        /// Indicates if the command should always be executed.
        /// </summary>
        bool AlwaysExecute { get; }

        /// <summary>
        /// Indicates if the argument processor is a primary action.
        /// </summary>
        bool IsAction { get; }

        /// <summary>
        /// Indicates the priority of the argument processor.
        /// The priority determines the order in which processors are initialized and executed.
        /// </summary>
        ArgumentProcessorPriority Priority { get; }

        /// <summary>
        /// The resource identifier for the Help Content associated with the decorated argument processor
        /// </summary>
        string HelpContentResourceName { get; }

        /// <summary>
        /// Based on this enum, corresponding help text will be shown.
        /// </summary>
        HelpContentPriority HelpPriority { get; }
    }
}
