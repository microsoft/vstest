// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Runtime.Versioning;

    /// <summary>
    /// Class for target Framework for the test container
    /// </summary>
    public class Framework
    {
#if NET46
        private static readonly Framework Default = Framework.FromString(".NETFramework,Version=v4.6");
#else
        private static readonly Framework Default = Framework.FromString(".NETCoreApp,Version=v1.0");
#endif

        private Framework()
        {
        }

        /// <summary>
        /// Default .Net target framework.
        /// </summary>
        public static Framework DefaultFramework => Framework.Default;

        /// <summary>
        /// Gets the FullName of framework.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the framework version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Returns a valid framework else returns null
        /// </summary>
        /// <param name="frameworkString">Framework name</param>
        /// <returns>A framework object</returns>
        public static Framework FromString(string frameworkString)
        {
            FrameworkName frameworkName;
            try
            {
                // FrameworkName already trims the input identifier.
                frameworkName = new FrameworkName(frameworkString);
            }
            catch
            {
                switch (frameworkString.Trim())
                {
                    case "Framework35":
                        frameworkName = new FrameworkName(".NETFramework,Version=v3.5");
                        break;
                    case "Framework40":
                        frameworkName = new FrameworkName(".NETFramework,Version=v4.0");
                        break;
                    case "Framework45":
                        frameworkName = new FrameworkName(".NETFramework,Version=v4.5");
                        break;
                    case "FrameworkCore10":
                        frameworkName = new FrameworkName(".NETCoreApp,Version=1.0");
                        break;
                    default:
                        return null;
                }
            }

            return new Framework() { Name = frameworkName.FullName, Version = frameworkName.Version.ToString() };
        }
        
        /// <summary>
        /// Returns full name of the framework.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
