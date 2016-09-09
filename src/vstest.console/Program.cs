// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Main entry point for the command line runner.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point. Hands off execution to the executor class.
        /// </summary>
        /// <param name="args">Arguments provided on the command line.</param>
        /// <returns>0 if everything was successful and 1 otherwise.</returns>
        public static int Main(string[] args)
        {
            return new Executor(ConsoleOutput.Instance).Execute(args);
        }
    }
}
