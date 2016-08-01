// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using DotNet.ProjectModel.Graph;
    using Microsoft.DotNet.ProjectModel;
    using NuGet.Packaging;
    using ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using TestPlatform.Utilities.Helpers;
    using TestPlatform.Utilities.Helpers.Interfaces;
    using Resources = Resources;

    /// <summary>
    /// Utility class for processing the json files
    /// </summary>
    public static class JsonUtilities
    {
        #region Constants/Static Members
        
        private const string DefaultConfiguration = "Debug";
        private const string JsonFileType = "json";

        private const string TestRunnerPrefix = "dotnet-test-";
        
        /// <summary>
        /// Project.FileName is an API of DotnetCore API
        /// Project refers to XPROJ container and FileName refers to "json" file name (project.json) 
        /// </summary>
        private static readonly string JsonFileName = Project.FileName;
        
        internal static IFileHelper FileHelper { get; set; } = new FileHelper();
        
        #endregion
        
        #region Private Methods
 
        /// <summary>
        /// Returns the dictionary of TestAdapterPaths and AssemblyPaths
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static Dictionary<string, IEnumerable<string>> GetTestRunnerAndAssemblyInfo(IEnumerable<string> sources)
        {
            var resultDictionary = new Dictionary<string, IEnumerable<string>>();

            foreach (var source in sources)
            {
                string assemblyPath = null;
                string testRunnerPath = null;
                if (source.EndsWith(JsonFileType))
                {
                    // Get the projectContext from the project Json file if exists
                    // User is expected to provide a full path to json folder, or path upto its directory
                    // If user provided nothing, try to find in current folder  
                    var projectContext = GetProjectContextFromSource(source);
                    
                    // Read data from created context
                    var testRunner = projectContext.ProjectFile.TestRunner;
                    
                    var options = CommandLineOptions.Instance;
                    var configuration = options.Configuration ?? DefaultConfiguration;
                    var outputpaths = projectContext.GetOutputPaths(configuration, options.BuildBasePath, options.Output);
                    assemblyPath = outputpaths.RuntimeFiles.Assembly;

                    testRunnerPath = GetTestRunnerPath(projectContext, string.Concat(TestRunnerPrefix, testRunner));

                    if (!FileHelper.Exists(assemblyPath))
                    {
                        throw new InvalidOperationException(Resources.AssemblyPathInvalid);
                    }
                }

                assemblyPath = assemblyPath ?? source;
                testRunnerPath = testRunnerPath ?? Constants.UnspecifiedAdapterPath;
                IEnumerable<string> assemblySources;
                if (resultDictionary.TryGetValue(testRunnerPath, out assemblySources))
                {
                    assemblySources = assemblySources.Concat(new List<string> { assemblyPath });
                    resultDictionary[testRunnerPath] = assemblySources;
                }
                else
                {
                    resultDictionary.Add(testRunnerPath, new List<string> { assemblyPath });
                }
            }
            return resultDictionary;
        }

        private static string GetTestRunnerPath(ProjectContext projectContext, string commandName)
        {
            var toolLibrary = projectContext.LockFile.Targets
                .FirstOrDefault(t => t.TargetFramework.GetShortFolderName()
                                      .Equals(projectContext.TargetFramework.GetShortFolderName()))
                ?.Libraries.FirstOrDefault(l => l.Name == commandName);

            var toolAssembly = toolLibrary?.RuntimeAssemblies
                  .FirstOrDefault(r => Path.GetFileNameWithoutExtension(r.Path) == commandName);

            var packageDirectory = new VersionFolderPathResolver(projectContext.PackagesDirectory)
                .GetInstallPath(toolLibrary.Name, toolLibrary.Version);

            return Path.Combine(packageDirectory, toolAssembly.Path);    
        }
        
        /// <summary>
        /// Returns the path to project.json file
        /// if User gave full path to "project.json" - do nothing
        /// If User gave path to a directory (use current dir if not) - append "project.json"
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        private static ProjectContext GetProjectContextFromSource(string source)
        {
            // user might not provide any value - use current directory 
            source = source ?? Directory.GetCurrentDirectory();

            // if user provided a directory or current dir - append "project.json" to path 
            // Example: C:\temp\ - will become - C:\temp\project.json
            if (!source.EndsWith(JsonFileName))
            {
                source = Path.Combine(source, JsonFileName);
            }
            
            // Verify if project.json exists
            if (!FileHelper.Exists(source))
            {
                throw new InvalidOperationException(string.Format(Resources.ProjectPathNotFound, source));
            }

            // Get default context from json 
            return ProjectContext.CreateContextForEachFramework(source).FirstOrDefault();
        }
        
        #endregion
    }
}
