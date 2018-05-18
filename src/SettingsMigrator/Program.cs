// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.SettingsMigrator
{
    using System;
    using System.Globalization;
    using System.IO;
    using CommandLineResources = Resources.Resources;

    /// <summary>
    /// Entry point for SettingsMigrator.
    /// </summary>
    public static class Program
    {
        private const string RunSettingsExtension = ".runsettings";

        private const string TestSettingsExtension = ".testsettings";

        /// <summary>
        /// Main entry point. Hands off execution to Migrator.
        /// </summary>
        /// <param name="args">Arguments provided on the command line.</param>
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ValidUsage));
                return;
            }

            string oldFilePath = args[0];

            if (!Path.IsPathRooted(oldFilePath))
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ValidUsage));
                return;
            }

            var newFileName = string.Concat(Guid.NewGuid().ToString(), RunSettingsExtension);
            string newFilePath = Path.Combine(Path.GetDirectoryName(oldFilePath), newFileName);

            var migrator = new Migrator();

            if (string.Equals(Path.GetExtension(oldFilePath), TestSettingsExtension))
            {
                migrator.MigrateTestSettings(oldFilePath, newFilePath);
            }
            else if (string.Equals(Path.GetExtension(oldFilePath), RunSettingsExtension))
            {
                migrator.MigrateRunSettings(oldFilePath, newFilePath);
            }
            else
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ValidUsage));
                return;
            }
        }
    }
}