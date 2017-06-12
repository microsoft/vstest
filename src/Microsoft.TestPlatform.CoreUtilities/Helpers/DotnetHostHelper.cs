// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Resources;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class DotnetHostHelper : IDotnetHostHelper
    {
        private readonly IFileHelper fileHelper;
        private readonly IEnvironment environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        public DotnetHostHelper() : this(new FileHelper(), new PlatformEnvironment())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        /// <param name="fileHelper">File Helper</param>
        public DotnetHostHelper(IFileHelper fileHelper, IEnvironment environment)
        {
            this.fileHelper = fileHelper;
            this.environment = environment;
        }

        /// <inheritdoc />
        public string GetDotnetHostFullPath()
        {
            var dotnetExeName = "dotnet.exe";

            if (!this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                dotnetExeName = "dotnet";
            }

            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(Path.PathSeparator))
            {
                string exeFullPath = Path.Combine(path.Trim(), dotnetExeName);
                if (this.fileHelper.Exists(exeFullPath))
                {
                    return exeFullPath;
                }
            }

            string errorMessage = string.Format(Resources.NoDotnetExeFound, dotnetExeName);

            EqtTrace.Error(errorMessage);
            throw new FileNotFoundException(errorMessage);
        }
    }
}
