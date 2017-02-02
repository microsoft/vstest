// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;

    internal class DotnetHostHelper : IDotnetHostHelper
    {
        private readonly IFileHelper fileHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        public DotnetHostHelper() : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        /// <param name="fileHelper">File Helper</param>
        public DotnetHostHelper(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        /// <inheritdoc />
        public string GetDotnetHostFullPath()
        {
            char separator = ';';
            var dotnetExeName = "dotnet.exe";

#if !NET46
            // Use semicolon(;) as path separator for windows
            // colon(:) for Linux and OSX
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                separator = ':';
                dotnetExeName = "dotnet";
            }
#endif

            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(separator))
            {
                string exeFullPath = Path.Combine(path.Trim(), dotnetExeName);
                if (this.fileHelper.Exists(exeFullPath))
                {
                    return exeFullPath;
                }
            }

            string errorMessage = String.Format(Resources.NoDotnetExeFound, dotnetExeName);
            EqtTrace.Error(errorMessage);
            throw new FileNotFoundException(errorMessage);
        }
    }
}
