// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VSIXCreator
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;

    public class Program
    {
        static List<string> filesNotToAddInManifestFile = new List<string>
        {
            "[Content_Types].xml",
            "catalog.json",
            "manifest.json"
        };

        public static void Main(string[] args)
        {
            var inputDirectory = "win7-x64";
            var outputDirectory = Environment.CurrentDirectory;
            if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
            {
                inputDirectory = args[0];
            }

            if (args.Length > 1 && !String.IsNullOrEmpty(args[1]))
            {
                outputDirectory = args[1];
            }

            var vsixFilePath = Path.Combine(outputDirectory, "TestPlatform.vsix");
            if (File.Exists(vsixFilePath))
            {
                File.Delete(vsixFilePath);
            }

            if (Directory.Exists(inputDirectory))
            {
                // Update the manifest file to have the entry for each file and their shah256 which are going to include in the vsix
                UpdateManifestFile(inputDirectory);

                // Get all the files which are going to be part of vsix
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

        /// <summary>
        /// Update the manifest file to have the entry for each file and their shah256 which are going to include in the vsix
        /// </summary>
        /// <param name="inputDirectory">The directory which contains the files</param>
        private static void UpdateManifestFile(string inputDirectory)
        {
            var list = new List<Dictionary<string, string>>();

            var allFiles = Directory.GetFiles(inputDirectory, "*.*", SearchOption.AllDirectories);

            int inputDirectoryLength = inputDirectory.Length;
            foreach (var file in allFiles)
            {
                if (!filesNotToAddInManifestFile.Contains(Path.GetFileName(file)))
                {
                    string fileRelativePath = file.Substring(inputDirectoryLength).Replace(@"\", @"/");

                    Dictionary<string, string> shaDictionary = new Dictionary<string, string>();
                    shaDictionary["fileName"] = fileRelativePath;
                    shaDictionary["sha256"] = GetChecksum(file);

                    list.Add(shaDictionary);
                }
            }

            string manifestFile = Path.Combine(inputDirectory, "manifest.json");
            string json = File.ReadAllText(manifestFile);
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["files"] = JToken.FromObject(list);

            json = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText(manifestFile, json);
        }

        /// <summary>
        /// Calculate sha256 string
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>sha256 string</returns>
        private static string GetChecksum(string file)
        {
            using (FileStream filestream = File.OpenRead(file))
            {
                using (var algorithm = SHA256.Create())
                {
                    var hash = algorithm.ComputeHash(filestream);

                    StringBuilder stringBuilder = new StringBuilder();

                    // Loop through each byte of the hashed data 
                    // and format each one as a hexadecimal string.
                    for (int i = 0; i < hash.Length; i++)
                    {
                        stringBuilder.Append(hash[i].ToString("x2"));
                    }

                    return stringBuilder.ToString();
                }
            }
        }
    }
}
