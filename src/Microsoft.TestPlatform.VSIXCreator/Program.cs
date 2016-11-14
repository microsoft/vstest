// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VSIXCreator
{
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    public class Program
    {
        public static void Main(string[] args)
        {
            var inputDirectory = "win7-x64";
            var outputDirectory = System.Environment.CurrentDirectory;
            if(args.Length > 0 && !String.IsNullOrEmpty(args[0]))
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

            if(System.IO.Directory.Exists(inputDirectory))
            {
                ZipFile.CreateFromDirectory(inputDirectory, vsixFilePath, CompressionLevel.Fastest, false);
            }      
        }
    }
}
