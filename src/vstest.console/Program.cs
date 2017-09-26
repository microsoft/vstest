// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console
{
    using System;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Main entry point for the command line runner.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point. Hands off execution to the command line library.
        /// </summary>
        /// <param name="args">Arguments provided on the command line.</param>
        /// <returns>0 if everything was successful and 1 otherwise.</returns>
        public static int Main(string[] args)
        {
#if NET451
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((sender, eventArgs) =>
            {
                var assemblyPath = Path.Combine("TestPlatform", new AssemblyName(eventArgs.Name).Name + ".dll");
                if (assemblyPath.Contains("Microsoft.VisualStudio.TestPlatform.ObjectModel.dll"))
                {
                    return System.Reflection.Assembly.LoadFrom(assemblyPath);
                }
                return null;
            });
            var objectModelPath = @"TestPlatform\Microsoft.VisualStudio.TestPlatform.ObjectModel.dll";
            if (File.Exists(objectModelPath))
            {
                System.Reflection.Assembly.LoadFrom(objectModelPath);
            }
#endif
            return Microsoft.TestPlatform.CommandLine.ConsoleRunner.Start(args);
        }
    }
}
