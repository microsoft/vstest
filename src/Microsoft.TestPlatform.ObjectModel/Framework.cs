// Copyright(c) Microsoft.All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Runtime.Versioning;

    /// <summary>
    /// Class for target Framework for the test container
    /// </summary>
    public class Framework
    {
        /// <summary>
        /// Default .Net target framework
        /// </summary>
        public static Framework DefaultFramework = FromString(".NETFramework,Version=v4.6");

        private Framework()
        {
        }
        
        /// <summary>
        /// FullName of the framework
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Version of the framework
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Returns a valid framework else returns null
        /// </summary>
        /// <param name="frameworkString"></param>
        /// <returns></returns>
        public static Framework FromString(string frameworkString)
        {
            FrameworkName frameworkName;
            try
            {
                frameworkName = new FrameworkName(frameworkString);
            }
            catch
            {
                switch(frameworkString)
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
                    default:
                        return null;
                }
            }
            return new Framework() { Name = frameworkName.FullName, Version = frameworkName.Version.ToString() };
        }
        
        /// <summary>
        /// Returns the Name which is the fullname of the framework.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
