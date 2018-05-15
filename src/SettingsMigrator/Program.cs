// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace SettingsMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [Full path to testsettings file/runsettings file to be migrated] [Full path to new runsettings file]");
                return;
            }

            string oldFilePath = args[0];
            string newFilePath = args[1];

            if (!Path.IsPathRooted(oldFilePath) || !Path.IsPathRooted(newFilePath) || !string.Equals(Path.GetExtension(newFilePath), RunsettingsExtension))
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [Full path to testsettings file/runsettings file to be migrated] [Full path to new runsettings file]");
                return;
            }

            var migrator = new Migrator();

            if (string.Equals(Path.GetExtension(oldFilePath), TestsettingsExtension))
            {
                migrator.MigrateTestsettings(oldFilePath, newFilePath, sampleRunsettingsContent);
            }
            else if (string.Equals(Path.GetExtension(oldFilePath), RunsettingsExtension))
            {
                migrator.MigrateRunsettings(oldFilePath, newFilePath);
            }
            else
            {
                Console.WriteLine("Valid usage:\nSettingsMigrator.exe [Full path to testsettings file/runsettings file to be migrated] [Full path to new runsettings file]");
                return;
            }
        }

        const string sampleRunsettingsContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                          "<RunSettings></RunSettings>";
        const string RunsettingsExtension = ".runsettings";
        const string TestsettingsExtension = ".testsettings";
    }
}

