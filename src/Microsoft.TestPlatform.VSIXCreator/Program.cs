// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VSIXCreator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    public class Program
    {
        public static void Main(string[] args)
        {
            var inputDirectory = "win7 -x64";
            var outputDirectory = System.Environment.CurrentDirectory;
            if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
            {
                inputDirectory = args[0];
            }

            if (args.Length > 1 && !String.IsNullOrEmpty(args[1]))
            {
                outputDirectory = args[1];
            }

            var vsixFilePath = System.IO.Path.Combine(outputDirectory, "TestPlatform.vsix");
            if (System.IO.File.Exists(vsixFilePath))
            {
                System.IO.File.Delete(vsixFilePath);
            }

            if (System.IO.Directory.Exists(inputDirectory))
            {
                // Get all files to put in vsix
                IEnumerable<string> files = Directory.EnumerateFiles(inputDirectory, "*.*", SearchOption.AllDirectories);
                int inputDirectoryLength = inputDirectory.Length;

                using (ZipArchive vsixFile = ZipFile.Open(vsixFilePath, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        if (!file.EndsWith(".pdb"))
                        {
                            // Creating same directory structure as of inputDirectory and replacing \ with / to make compatible with V3 format.
                            // because if we create vsix with \ then at time of installation sub folder does not get copied.
                            string addFile = file.Substring(inputDirectoryLength + 1).Replace(@"\", @"/");
                            vsixFile.CreateEntryFromFile(file, addFile, CompressionLevel.Optimal);
                        }
                    }
                }
            }
        }
    }
}
