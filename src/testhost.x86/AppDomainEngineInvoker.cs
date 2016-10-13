namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
#if NET46
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Implementation for the Invoker which invokes engine in a new AppDomain
    /// Type of the engine must be a marshalable object for app domain calls and also must have a parameterless constructor
    /// </summary>
    internal class AppDomainEngineInvoker<T> : IEngineInvoker where T : MarshalByRefObject, IEngineInvoker, new()
    {
        private const string XmlNamespace = "urn:schemas-microsoft-com:asm.v1";

        private readonly string testSourcePath;

        public AppDomainEngineInvoker(string testSourcePath)
        {
            this.testSourcePath = testSourcePath;
        }

        /// <summary>
        /// Invokes the Engine with the arguments
        /// </summary>
        /// <param name="argsDictionary">Arguments for the engine</param>
        public void Invoke(IDictionary<string, string> argsDictionary)
        {
            string mergedTempConfigFile = null;
            try
            {
                var invoker = CreateInvokerInAppDomain(testSourcePath, out mergedTempConfigFile);

                EqtTrace.Info("AppDomainEngineInvoker: Invoking Actual Engine.");
                invoker.Invoke(argsDictionary);
            }
            finally
            {
                try
                {
                    if (mergedTempConfigFile != null && File.Exists(mergedTempConfigFile))
                    {
                        File.Delete(mergedTempConfigFile);
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("AppDomainEngineInvoker: Error occured while trying to delete a temp file: {0}, Exception: {1}", mergedTempConfigFile, ex);
                }
            }
        }

        /// <summary>
        /// Create the Engine Invoker in new AppDomain based on test source path
        /// </summary>
        /// <param name="testSourcePath">Test Source to run/discover tests for</param>
        /// <param name="mergedConfigFile">Merged config file if there is any merging of test config and test host config</param>
        /// <returns></returns>
        private IEngineInvoker CreateInvokerInAppDomain(string testSourcePath, out string mergedConfigFile)
        {
            var appDomainSetup = new AppDomainSetup();

            var testSourceFolder = Path.GetDirectoryName(testSourcePath);
            EqtTrace.Info("AppDomainEngineInvoker: Using '{0}' as AppBase for new AppDomain.", testSourceFolder);

            // Set AppBase to TestAssembly location
            appDomainSetup.ApplicationBase = testSourceFolder;
            appDomainSetup.LoaderOptimization = LoaderOptimization.MultiDomainHost;

            // Set User Config file as app domain config
            SetConfigurationFile(appDomainSetup, testSourcePath, testSourceFolder, out mergedConfigFile);

            EqtTrace.Info("AppDomainEngineInvoker: Creating new appdomain");
            // Create new AppDomain
            var appDomain = AppDomain.CreateDomain("TestHostAppDomain", null, appDomainSetup);

            // Create Custom assembly resolver in new appdomain before anything else to resolve testplatform assemblies
            appDomain.CreateInstanceFromAndUnwrap(
                    typeof(CustomAssemblyResolver).Assembly.Location,
                    typeof(CustomAssemblyResolver).FullName,
                    false,
                    BindingFlags.Default,
                    null,
                    new object[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) },
                    null,
                    null);

            var invokerType = typeof(T);
            EqtTrace.Info("AppDomainEngineInvoker: Creating Invoker of type '{0}' in new AppDomain.", invokerType);

            // Create Invoker object in new appdomain
            return (IEngineInvoker) appDomain.CreateInstanceFromAndUnwrap(
                    invokerType.Assembly.Location,
                    invokerType.FullName,
                    false,
                    BindingFlags.Default,
                    null,
                    null,
                    null,
                    null);
        }

        private static void SetConfigurationFile(AppDomainSetup appDomainSetup, string testSource, string testSourceFolder, out string mergedConfigFile)
        {
            var configFile = GetConfigFile(testSource, testSourceFolder);
            EqtTrace.Info("AppDomainEngineInvoker: User Configuration file '{0}' for testSource '{1}'", configFile, testSource);

            mergedConfigFile = null;
            var testHostAppConfigFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            if (!string.IsNullOrEmpty(configFile))
            {
                EqtTrace.Info("AppDomainEngineInvoker: Merging test configuration file '{0}' and TestHost configuration file '{1}'", configFile, testHostAppConfigFile);

                // Merge user's config file and testHost config file and use merged one 
                mergedConfigFile = MergeApplicationConfigFiles(configFile, testHostAppConfigFile);

                EqtTrace.Info("AppDomainEngineInvoker: Using merged configuration file: {0}.", mergedConfigFile);
                appDomainSetup.ConfigurationFile = mergedConfigFile;
            }
            else
            {
                EqtTrace.Info("AppDomainEngineInvoker: No User configuration file found. Using TestHost Config File: {0}.", testHostAppConfigFile);

                // Use the current domains configuration setting.
                appDomainSetup.ConfigurationFile = testHostAppConfigFile;
            }
        }

        private static string GetConfigFile(string testSource, string testSourceFolder)
        {
            string configFile = null;

            if (File.Exists(testSource + ".config"))
            {
                // Path to config file cannot be bad: storage is already checked, and extension is valid.
                configFile = testSource + ".config";
            }
            else
            {
                var netAppConfigFile = Path.Combine(testSourceFolder, "App.Config");
                if (File.Exists(netAppConfigFile))
                {
                    configFile = netAppConfigFile;
                }
            }

            return configFile;
        }

        private static string MergeApplicationConfigFiles(string userConfigFile, string testHostConfigFile)
        {
            var userConfigDoc = XDocument.Load(userConfigFile);
            var testHostConfigDoc = XDocument.Load(testHostConfigFile);

            // Start with User's config file as the base
            var mergedDoc = new XDocument(userConfigDoc);

            // Take testhost.exe Startup node 
            var startupNode = testHostConfigDoc.Descendants("startup")?.FirstOrDefault();
            if (startupNode != null)
            {
                // Remove user's startup and add ours which supports NET35 
                mergedDoc.Descendants("startup")?.Remove();
                mergedDoc.Root.Add(startupNode);
            }

            // Runtime node must be merged which contains assembly redirections
            var runtimeTestHostNode = testHostConfigDoc.Descendants("runtime")?.FirstOrDefault();
            if (runtimeTestHostNode != null)
            {
                // remove test host relative probing paths' element
                // TestHost Probing Paths do not make sense since we are setting "AppBase" to user's test assembly location
                runtimeTestHostNode.Descendants().Where((element) => string.Equals(element.Name.LocalName, "probing")).Remove();

                var runTimeNode = mergedDoc.Descendants("runtime")?.FirstOrDefault();
                if (runTimeNode == null)
                {
                    // no runtime node exists in user's config - just add ours entirely
                    mergedDoc.Root.Add(runtimeTestHostNode);
                }
                else
                {
                    var assemblyBindingXName = XName.Get("assemblyBinding", XmlNamespace);
                    var mergedDocAssemblyBindingNode = mergedDoc.Descendants(assemblyBindingXName)?.FirstOrDefault();
                    var testHostAssemblyBindingNode = runtimeTestHostNode.Descendants(assemblyBindingXName)?.FirstOrDefault();

                    if (testHostAssemblyBindingNode != null)
                    {
                        if (mergedDocAssemblyBindingNode == null)
                        {
                            // add another assemblyBinding element as none exists in user's config
                            runTimeNode.Add(testHostAssemblyBindingNode);
                        }
                        else
                        {
                            var dependentAssemblyXName = XName.Get("dependentAssembly", XmlNamespace);
                            var redirections = testHostAssemblyBindingNode.Descendants(dependentAssemblyXName);

                            if (redirections != null)
                            {
                                mergedDocAssemblyBindingNode.Add(redirections);
                            }
                        }
                    }
                }
            }

            var tempFile = Path.GetTempFileName();
            mergedDoc.Save(tempFile);

            return tempFile;
        }
    }

    /// <summary>
    /// Custom Assembly resolver for child app domain to resolve testplatform assemblies
    /// </summary>
    internal class CustomAssemblyResolver : MarshalByRefObject
    {
        private readonly IDictionary<string, Assembly> resolvedAssemblies;

        private readonly string[] resolverPaths;

        public CustomAssemblyResolver(string testPlatformPath)
        {
            this.resolverPaths = new string[] { testPlatformPath, Path.Combine(testPlatformPath, "Extensions") };
            this.resolvedAssemblies = new Dictionary<string, Assembly>();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            Assembly assembly = null;
            lock (resolvedAssemblies)
            {
                try
                {
                    EqtTrace.Verbose("CurrentDomain_AssemblyResolve: Resolving assembly '{0}'.", args.Name);

                    if (resolvedAssemblies.TryGetValue(args.Name, out assembly))
                    {
                        return assembly;
                    }

                    // Put it in the resolved assembly so that if below Assembly.Load call
                    // triggers another assembly resolution, then we dont end up in stack overflow
                    resolvedAssemblies[args.Name] = null;

                    foreach (var path in resolverPaths)
                    {
                        var testPlatformFilePath = Path.Combine(path, assemblyName.Name) + ".dll";
                        if (File.Exists(testPlatformFilePath))
                        {
                            try
                            {
                                assembly = Assembly.LoadFrom(testPlatformFilePath);
                                break;
                            }
                            catch (Exception)
                            {
                                // ignore
                            }
                        }
                    }

                    // Replace the value with the loaded assembly
                    resolvedAssemblies[args.Name] = assembly;

                    return assembly;
                }
                finally
                {
                    if (null == assembly)
                    {
                        EqtTrace.Verbose("CurrentDomainAssemblyResolve: Failed to resolve assembly '{0}'.", args.Name);
                    }
                }
            }
        }
    }
#endif
}
