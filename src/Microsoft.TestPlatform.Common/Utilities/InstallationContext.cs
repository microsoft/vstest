// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class InstallationContext
    {
        private const string DevenvExe = "devenv.exe";
        private const string PrivateAssembliesDirName = "PrivateAssemblies";
        private const string PublicAssembliesDirName = "PublicAssemblies";

        private readonly IFileHelper fileHelper;

        public InstallationContext(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        public bool TryGetVisualStudioDirectory(out string visualStudioDirectory)
        {
            var vsInstallPath = new DirectoryInfo(typeof(InstallationContext).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent?.Parent?.Parent?.FullName;
            if (!string.IsNullOrEmpty(vsInstallPath))
            {
                var pathToDevenv = Path.Combine(vsInstallPath, DevenvExe);
                if (!string.IsNullOrEmpty(pathToDevenv) && this.fileHelper.Exists(pathToDevenv))
                {
                    visualStudioDirectory = vsInstallPath;
                    return true;
                }
            }

            visualStudioDirectory = string.Empty;
            return false;
        }

        public string GetVisualStudioPath(string visualStudioDirectory)
        {
            return Path.Combine(visualStudioDirectory, DevenvExe);
        }

        public string[] GetVisualStudioCommonLocations(string visualStudioDirectory)
        {
            return new[]
            {
                Path.Combine(visualStudioDirectory, PrivateAssembliesDirName),
                Path.Combine(visualStudioDirectory, PublicAssembliesDirName),
                Path.Combine(visualStudioDirectory, "CommonExtensions", "Microsoft", "TestWindow"),
                visualStudioDirectory
            };
        }
    }
}
