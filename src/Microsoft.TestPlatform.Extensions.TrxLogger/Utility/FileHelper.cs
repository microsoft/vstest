// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Helper function to deal with file name.
    /// </summary>
    internal static class FileHelper
    {
        private const string RelativeDirectorySeparator = "..";

        private static readonly Dictionary<char, object> InvalidFileNameChars;
        private static readonly Dictionary<char, object> AdditionalInvalidFileNameChars;
        private static readonly Regex ReservedFileNamesRegex = new Regex(@"(?i:^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]|CLOCK\$)(\..*)?)$");

        #region Constructors
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Reviewed. Suppression is OK here.")]

        // Have to init InvalidFileNameChars dynamically.
        static FileHelper()
        {
            // Create a hash table of invalid chars.
            char[] invalidCharsArray = Path.GetInvalidFileNameChars();
            InvalidFileNameChars = new Dictionary<char, object>(invalidCharsArray.Length);
            foreach (char c in invalidCharsArray)
            {
                InvalidFileNameChars.Add(c, null);
            }

            // Needed becuase when kicking off qtsetup.bat cmd.exe is used.  '@' is a special character
            // for cmd so must be removed from the path to the bat file
            AdditionalInvalidFileNameChars = new Dictionary<char, object>(4);
            AdditionalInvalidFileNameChars.Add('@', null);
            AdditionalInvalidFileNameChars.Add('(', null);
            AdditionalInvalidFileNameChars.Add(')', null);
            AdditionalInvalidFileNameChars.Add('^', null);
        }

        #endregion

        /// <summary>
        /// Replaces invalid file name chars in the specified string and changes it if it is a reserved file name.
        /// </summary>
        /// <param name="fileName">the name of the file</param>
        /// <returns>Replaced string.</returns>
        public static string ReplaceInvalidFileNameChars(string fileName)
        {
            EqtAssert.StringNotNullOrEmpty(fileName, "fileName");

            // Replace bad chars by this.
            char replacementChar = '_'; 
            StringBuilder result = new StringBuilder(fileName.Length);
            result.Length = fileName.Length;

            // Replace each invalid char with replacement char.
            for (int i = 0; i < fileName.Length; ++i)
            {
                if (InvalidFileNameChars.ContainsKey(fileName[i]) ||
                    AdditionalInvalidFileNameChars.ContainsKey(fileName[i]))
                {
                    result[i] = replacementChar;
                }
                else
                {
                    result[i] = fileName[i];
                }
            }

            // We trim spaces in the end because CreateFile/Dir trim those.
            string replaced = result.ToString().TrimEnd(); 
            if (replaced.Length == 0)
            {
                Debug.Fail(string.Format(CultureInfo.InvariantCulture, "After replacing invalid chars in file '{0}' there's nothing left...", fileName));
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_NothingLeftAfterReplaciingBadCharsInName, fileName));
            }

            if (IsReservedFileName(replaced))
            {
                replaced = replacementChar + replaced;  // Cannot add to the end because it can have extensions.
            }

            return replaced;
        }

        /// <summary>
        /// Checks whether file with specified name exists in the specified directory.
        /// If it exits, adds (1),(2)... to the file name and checks again.
        /// Returns full file name (with path) of the iteration when the file does not exist.
        /// </summary>
        /// <param name="parentDirectoryName">
        /// The directory where to check.
        /// </param>
        /// <param name="originalFileName">
        /// The original file (that we would add (1),(2),.. in the end of if needed) name to check.
        /// </param>
        /// <param name="checkMatchingDirectory">
        /// If true, and directory with filename without extension exists, try next iteration.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetNextIterationFileName(string parentDirectoryName, string originalFileName, bool checkMatchingDirectory)
        {
            EqtAssert.StringNotNullOrEmpty(parentDirectoryName, "parentDirectoryName");
            EqtAssert.StringNotNullOrEmpty(originalFileName, "originalFileName");
            return GetNextIterationNameHelper(parentDirectoryName, originalFileName, new FileIterationHelper(checkMatchingDirectory));
        }

        public static string MakePathRelative(string path, string basePath)
        {
            EqtAssert.StringNotNullOrEmpty(path, "path");

            // Can't be relative to nothing
            if (string.IsNullOrEmpty(basePath))
            {
                return path;
            }

            // Canonicalize those paths:

            if (!Path.IsPathRooted(path))
            {
                //If path is relative, we combine it with base path before canonicalizing.
                //Else Path.GetFullPath is going to use the process worker directory (e.g. e:\binariesy.x86\bin\i386).
                path = Path.Combine(basePath, path);
            }
            path = Path.GetFullPath(path);
            basePath = Path.GetFullPath(basePath);

            char[] delimiters = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            basePath = basePath.TrimEnd(delimiters);
            path = path.TrimEnd(delimiters);

            string[] pathTokens = path.Split(delimiters);
            string[] basePathTokens = basePath.Split(delimiters);

            Debug.Assert(pathTokens.Length > 0 && basePathTokens.Length > 0);
            int max = Math.Min(pathTokens.Length, basePathTokens.Length);

            // Skip all of the empty tokens that result from things like "\dir1"
            // and "\\dir1". We need to compare the first non-null token
            // to know if we've got differences.
            int i = 0;
            for (i = 0; i < max && pathTokens[i].Length == 0 && basePathTokens[i].Length == 0; i++) ;

            if (i >= max)
            {
                // At least one of these strings is too short to work with
                return path;
            }

            if (!pathTokens[i].Equals(basePathTokens[i], StringComparison.OrdinalIgnoreCase))
            {
                // These differ from the very start - just return the original path
                return path;
            }

            for (++i; i < max; i++)
            {
                if (!pathTokens[i].Equals(basePathTokens[i], StringComparison.OrdinalIgnoreCase))
                {
                    // We've found a non-matching token
                    break;
                }
            }

            // i should point to first non-matching token.

            StringBuilder newPath = new StringBuilder();

            // ok, for each remaining token in the base path,
            // add ..\ to the string.
            for (int j = i; j < basePathTokens.Length; j++)
            {
                if (newPath.Length > 0)
                {
                    newPath.Append(Path.DirectorySeparatorChar);
                }
                newPath.Append(RelativeDirectorySeparator);
            }

            // And now, for every remaining token in the path,
            // add it to the string, separated by the directory
            // separator.

            for (int j = i; j < pathTokens.Length; j++)
            {
                if (newPath.Length > 0)
                {
                    newPath.Append(Path.DirectorySeparatorChar);
                }
                newPath.Append(pathTokens[j]);
            }

            return newPath.ToString();
        }

        /// <summary>
        /// Returns true if the file name specified is Windows reserved file name.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file. Note: only a file name, does not expect to contain directory separators.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/> True if yes else False.
        /// </returns>
        private static bool IsReservedFileName(string fileName)
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

        /// <summary>
        /// Helper to get next iteration (1),(2),.. names.
        /// Note that we don't check for security permissions:
        ///     If the file exists and you have access to the dir you will get File.Exist = true
        ///     If the file exists and you don't have access to the dir, you will not be able to create the file anyway.
        /// Result.trx -&gt; Result(1).trx, Result(2).trx, etc.
        /// </summary>
        /// <param name="baseDirectoryName">
        /// Base directory to try iteration in.
        /// </param>
        /// <param name="originalName">
        /// The name to start the iterations from.
        /// </param>
        /// <param name="helper">
        /// An instance of IterationHelper.
        /// </param>
        /// <returns>
        /// Next valid iteration name.
        /// </returns>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        private static string GetNextIterationNameHelper(
            string baseDirectoryName,
            string originalName,
            IterationHelper helper)
        {
            Debug.Assert(!string.IsNullOrEmpty(baseDirectoryName), "baseDirectoryname is null");
            Debug.Assert(!string.IsNullOrEmpty(originalName), "originalName is Null");
            Debug.Assert(helper != null, "helper is null");

            uint iteration = 0;
            do
            {
                var tryMe = iteration == 0 ? originalName : helper.NextIteration(originalName, iteration);

                string tryMePath = Path.Combine(baseDirectoryName, tryMe);
                if (helper.IsValidIteration(tryMePath))
                {
                    return tryMePath;
                }

                ++iteration;
            }
            while (iteration != uint.MaxValue);

            throw new Exception(string.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_CannotGetNextIterationName, originalName, baseDirectoryName));
        }

        private abstract class IterationHelper
        {
            /// <summary>
            /// Formats iteration like baseName[1].
            /// </summary>
            /// <param name="baseName">
            /// Base name for the iteration.
            /// </param>
            /// <param name="iteration">
            /// The iteration number
            /// </param>
            /// <returns>
            /// The formatted string.
            /// </returns>
            internal static string FormatIteration(string baseName, uint iteration)
            {
                Debug.Assert(!string.IsNullOrEmpty(baseName), "basename is null");

                var tryMe = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}[{1}]",
                    baseName,
                    iteration.ToString(CultureInfo.InvariantCulture));
                return tryMe;
            }

            internal abstract string NextIteration(string baseName, uint iteration);

            internal abstract bool IsValidIteration(string name);
        }

        private class FileIterationHelper : IterationHelper
        {
            private readonly bool checkMatchingDirectory;

            /// <summary>
            /// Constructor for class checkMatchingDirectory.
            /// </summary>
            /// <param name="checkMatchingDirectory">If true, and directory with filename without extension exists, try next iteration.</param>
            internal FileIterationHelper(bool checkMatchingDirectory)
            {
                this.checkMatchingDirectory = checkMatchingDirectory;
            }

            internal override string NextIteration(string baseName, uint iteration)
            {
                Debug.Assert(!string.IsNullOrEmpty(baseName), "baseName is null");

                string withoutExtensionName = Path.GetFileNameWithoutExtension(baseName);
                string tryMe = FormatIteration(withoutExtensionName, iteration);
                if (Path.HasExtension(baseName))
                {
                    tryMe += Path.GetExtension(baseName);   // Path.GetExtension already returns the leading ".".
                }

                return tryMe;
            }

            internal override bool IsValidIteration(string path)
            {
                Debug.Assert(!string.IsNullOrEmpty(path), "path is null");
                if (File.Exists(path) || Directory.Exists(path))
                {
                    return false;
                }

                // Path.ChangeExtension for "" returns trailing dot but Directory.Exists works the same for dir with and without trailing dot.
                if (this.checkMatchingDirectory && Path.HasExtension(path) && Directory.Exists(Path.ChangeExtension(path, string.Empty)))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
