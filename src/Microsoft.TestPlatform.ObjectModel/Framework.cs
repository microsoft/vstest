// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Runtime.Versioning;

    /// <summary>
    /// Class for target Framework for the test container
    /// </summary>
    public class Framework
    {
#if NET451
        private static readonly Framework Default = Framework.FromString(".NETFramework,Version=v4.0");
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
            if (string.IsNullOrWhiteSpace(frameworkString))
            {
                return null;
            }

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
                        frameworkName = new FrameworkName(Constants.DotNetFramework35);
                        break;
                    case "Framework40":
                        frameworkName = new FrameworkName(Constants.DotNetFramework40);
                        break;
                    case "Framework45":
                        frameworkName = new FrameworkName(Constants.DotNetFramework45);
                        break;
                    case "FrameworkCore10":
                        frameworkName = new FrameworkName(Constants.DotNetFrameworkCore10);
                        break;
                    case "FrameworkUap10":
                        frameworkName = new FrameworkName(Constants.DotNetFrameworkUap10);
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

        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                base.Equals(obj);
            }
            return this.Version.ToUpper().Equals(((Framework)obj).Version.ToUpper());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
