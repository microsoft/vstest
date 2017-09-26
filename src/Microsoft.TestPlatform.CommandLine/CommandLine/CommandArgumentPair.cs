// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine
{
    using System;
    using System.Diagnostics.Contracts;

    using CommandLineResources = Microsoft.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Breaks a string down into command and argument based on the following format:
    ///     /command:argument.
    /// </summary>
    internal class CommandArgumentPair
    {
        #region Constants

        /// <summary>
        /// The separator.
        /// </summary>
        internal const string Separator = ":";

        #endregion

        #region Properties

        /// <summary>
        /// The command portion of the input.
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// The argument portion of the input.
        /// </summary>
        public string Argument { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Breaks the provided command line switch into the command and argument pair.
        /// </summary>
        /// <param name="input">Input to break up.</param>
        public CommandArgumentPair(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException(CommandLineResources.CannotBeNullOrEmpty, "input");
            }
            Contract.Ensures(!String.IsNullOrWhiteSpace(Command));

            this.Parse(input);
        }

        /// <summary>
        /// Stores the provided command and argument pair.
        /// </summary>
        /// <param name="command">The command portion of the input.</param>
        /// <param name="argument">The argument portion of the input.</param>
        public CommandArgumentPair(string command, string argument)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException(CommandLineResources.CannotBeNullOrEmpty, "command");
            }

            Contract.Ensures(Command == command);
            Contract.Ensures(Argument == argument);

            this.Command = command;
            this.Argument = argument;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses the input into the command and argument parts.
        /// </summary>
        /// <param name="input">Input string to parse.</param>
        private void Parse(string input)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(input));
            Contract.Ensures(!String.IsNullOrWhiteSpace(Command));
            Contract.Ensures(Argument != null);

            // Find the index of the seperator (":")
            int index = input.IndexOf(CommandArgumentPair.Separator, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                // No seperator was found, so use the input as the command.
                this.Command = input;
                this.Argument = String.Empty;
            }
            else
            {
                // Separator was found, so separate the command and the input.
                this.Command = input.Substring(0, index);
                this.Argument = input.Substring(index + 1);
            }
        }

#endregion
    }
}
