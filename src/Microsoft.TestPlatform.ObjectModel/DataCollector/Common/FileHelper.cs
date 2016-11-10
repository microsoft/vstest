// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text.RegularExpressions;

    internal sealed class FileHelper
    {
        private static Dictionary<char, object> invalidFileNameChars;
        private static Regex ReservedFileNamesRegex = new Regex(@"(?i:^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]|CLOCK\$)(\..*)?)$");

        #region Constructors
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]  // Have to init invalidFileNameChars dynamically.
        static FileHelper()
        {
            // Create a hash table of invalid chars.
            char[] invalidCharsArray = Path.GetInvalidFileNameChars();
            invalidFileNameChars = new Dictionary<char, object>(invalidCharsArray.Length);
            foreach (char c in invalidCharsArray)
            {
                invalidFileNameChars.Add(c, null);
            }
        }

        private FileHelper()
        {
        }
        #endregion

        #region Fields
        /// <summary>
        /// Determines if a file name has invalid characters.
        /// </summary>
        /// <param name="fileName">File name to check.</param>
        /// <param name="invalidCharacters">Invalid characters which were found in the file name.</param>
        /// <returns>True if the file name is valid and false if the filename contains invalid characters.</returns>
        public static bool IsValidFileName(string fileName, out string invalidCharacters)
        {
            bool result = true;
            //EqtAssert.StringNotNullOrEmpty(fileName, "fileName");

            // Find all of the invalid characters in the file name.
            invalidCharacters = null;
            for (int i = 0; i < fileName.Length; i++)
            {
                if (invalidFileNameChars.ContainsKey(fileName[i]))
                {
                    invalidCharacters = String.Concat(invalidCharacters, fileName[i]);
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the file name specified is Windows reserved file name.
        /// </summary>
        /// <param name="fileName">The name of the file. Note: only a file name, does not expect to contain dir separators.</param>
        internal static bool IsReservedFileName(string fileName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName), "FileHelper.IsReservedFileName: the argument is null or empty string!");
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            // CreateFile:
            // The following reserved device names cannot be used as the name of a file: 
            // CON, PRN, AUX, NUL, COM1, COM2, COM3, COM4, COM5, COM6, COM7, COM8, COM9, 
            // LPT1, LPT2, LPT3, LPT4, LPT5, LPT6, LPT7, LPT8, and LPT9. 
            // Also avoid these names followed by an extension, for example, NUL.tx7.
            // Windows NT: CLOCK$ is also a reserved device name.
            return ReservedFileNamesRegex.Match(fileName).Success;
        }
        
        #endregion
    }
}
