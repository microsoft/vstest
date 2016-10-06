using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
#if NET46
    internal static class AppDomainHelper
    {
        private const string ObjectModelVersionBuiltAgainst = "14.0.0.0";

        private const string XmlNamespace = "urn:schemas-microsoft-com:asm.v1";

        public static AppDomain CreateAppDomain(string testAssembly)
        {
            var appDomainSetup = new AppDomainSetup();
            appDomainSetup.ApplicationBase = Path.GetDirectoryName(Path.GetFullPath(testAssembly));
            appDomainSetup.LoaderOptimization = LoaderOptimization.MultiDomainHost;

            SetConfigurationFile(appDomainSetup, testAssembly);

            var appDomain = AppDomain.CreateDomain("TestHostAppDomainForTestAssembly:" + testAssembly, null, appDomainSetup);

            // This is done so that ObjectModel is loaded first in the new AppDomain before any other adapter assembly is used
            CreateObjectInNewDomain<SettingsException>(appDomain);

            CreateObjectInNewDomain<CustomAssemblyResolver>(appDomain, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            return appDomain;
        }

        internal static T CreateObjectInNewDomain<T>(AppDomain appDomain, params object[] args)
        {
            var invokerType = typeof(T);
            var invoker = appDomain.CreateInstanceFromAndUnwrap(
                    invokerType.Assembly.Location,
                    invokerType.FullName,
                    false,
                    BindingFlags.Default,
                    null,
                    args,
                    null,
                    null);
            return (T)invoker;
        }

        internal static void SetConfigurationFile(AppDomainSetup appDomainSetup, string testSource)
        {
            var configFile = GetConfigFile(testSource);

            if (!string.IsNullOrEmpty(configFile))
            {
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("UnitTestAdapter: Using configuration file {0} for testSource {1}.", configFile, testSource);
                }
                appDomainSetup.ConfigurationFile = Path.GetFullPath(configFile);

                try
                {
                    // Add redirection of the built 14.0 Object Model assembly to the current version if that is not 14.0
                    var currentVersionOfObjectModel = typeof(TestCase).Assembly.GetName().Version.ToString();
                    if (!string.Equals(currentVersionOfObjectModel, ObjectModelVersionBuiltAgainst))
                    {
                        var configurationBytes = AddObjectModelRedirectAndConvertToByteArray(configFile);
                        appDomainSetup.SetConfigurationBytes(configurationBytes);
                    }
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("Exception hit while adding binding redirects to test source config file. Exception : {0}", ex);
                    }
                }
            }
            else
            {
                // Use the current domains configuration setting.
                appDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            }
        }

        private static byte[] AddObjectModelRedirectAndConvertToByteArray(string configFile)
        {
            var doc = new XmlDocument();
            if (!string.IsNullOrEmpty(configFile?.Trim()))
            {
                using (var xmlReader = new XmlTextReader(configFile))
                {
                    xmlReader.DtdProcessing = DtdProcessing.Prohibit;
                    xmlReader.XmlResolver = null;
                    doc.Load(xmlReader);
                }
            }
            var configurationElement = FindOrCreateElement(doc, doc, "configuration");
            var assemblyBindingSection = FindOrCreateAssemblyBindingSection(doc, configurationElement);
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            assemblyName.Name = "Microsoft.VisualStudio.TestPlatform.ObjectModel";
            var currentVersion = typeof(TestCase).Assembly.GetName().Version.ToString();
            AddAssemblyBindingRedirect(doc, assemblyBindingSection, assemblyName, ObjectModelVersionBuiltAgainst, currentVersion);
            using (var ms = new MemoryStream())
            {
                doc.Save(ms);
                return ms.ToArray();
            }
        }

        private static XmlElement FindOrCreateAssemblyBindingSection(XmlDocument doc, XmlElement configurationElement)
        {
            // Each section must be created with the xmlns specified so that
            // we don't end up with xmlns="" on each element.

            // Find or create the runtime section (this one should not have an xmlns on it).
            var runtimeSection = FindOrCreateElement(doc, configurationElement, "runtime");

            // Use the assemblyBinding section if it exists; otherwise, create one.
            var assemblyBindingSection = runtimeSection["assemblyBinding"];
            if (assemblyBindingSection != null)
            {
                return assemblyBindingSection;
            }
            assemblyBindingSection = doc.CreateElement("assemblyBinding", XmlNamespace);
            runtimeSection.AppendChild(assemblyBindingSection);
            return assemblyBindingSection;
        }

        /// <summary>
        /// Add an assembly binding redirect entry to the config file.
        /// </summary>
        /// <param name="doc"> The doc. </param>
        /// <param name="assemblyBindingSection"> The assembly Binding Section. </param>
        /// <param name="assemblyName"> The assembly Name. </param>
        /// <param name="fromVersion"> The from Version. </param>
        /// <param name="toVersion"> The to Version. </param>
        private static void AddAssemblyBindingRedirect(XmlDocument doc, XmlElement assemblyBindingSection,
            AssemblyName assemblyName,
            string fromVersion,
            string toVersion)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }


            // Convert the public key token into a string.
            StringBuilder publicKeyTokenString = null;
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            if (null != publicKeyToken)
            {
                publicKeyTokenString = new StringBuilder(publicKeyToken.GetLength(0) * 2);
                for (var i = 0; i < publicKeyToken.GetLength(0); i++)
                {
                    publicKeyTokenString.AppendFormat(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0:x2}",
                        new object[] { publicKeyToken[i] });
                }
            }

            // Get the culture as a string.
            var cultureString = assemblyName.CultureInfo.ToString();
            if (string.IsNullOrEmpty(cultureString))
            {
                cultureString = "neutral";
            }

            // Add the dependentAssembly section.
            var dependentAssemblySection = doc.CreateElement("dependentAssembly", XmlNamespace);
            assemblyBindingSection.AppendChild(dependentAssemblySection);

            // Add the assemblyIdentity element.
            var assemblyIdentityElement = doc.CreateElement("assemblyIdentity", XmlNamespace);
            assemblyIdentityElement.SetAttribute("name", assemblyName.Name);
            if (null != publicKeyTokenString)
            {
                assemblyIdentityElement.SetAttribute("publicKeyToken", publicKeyTokenString.ToString());
            }
            assemblyIdentityElement.SetAttribute("culture", cultureString);
            dependentAssemblySection.AppendChild(assemblyIdentityElement);

            var bindingRedirectElement = doc.CreateElement("bindingRedirect", XmlNamespace);
            bindingRedirectElement.SetAttribute("oldVersion", fromVersion);
            bindingRedirectElement.SetAttribute("newVersion", toVersion);
            dependentAssemblySection.AppendChild(bindingRedirectElement);
        }

        private static XmlElement FindOrCreateElement(XmlDocument doc, XmlNode parent, string name)
        {
            var ret = parent[name];

            if (ret != null)
            {
                return ret;
            }

            ret = doc.CreateElement(name, parent.NamespaceURI);
            parent.AppendChild(ret);
            return ret;
        }

        private static string GetConfigFile(string testSource)
        {
            string configFile = null;

            if (File.Exists(testSource + ".config"))
            {
                // Path to config file cannot be bad: storage is already checked, and extension is valid.
                configFile = testSource + ".config";
            }
            else
            {
                var netAppConfigFile = Path.Combine(Path.GetDirectoryName(testSource), "App.Config");
                if (File.Exists(netAppConfigFile))
                {
                    configFile = netAppConfigFile;
                }
            }
            return configFile;
        }
    }

    internal class CustomAssemblyResolver : MarshalByRefObject
    {
        private readonly IDictionary<string, Assembly> resolvedAssemblies;

        private readonly string testPlatformPath;

        public CustomAssemblyResolver(string testPlatformPath)
        {
            this.testPlatformPath = testPlatformPath;
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

                    var testPlatformFilePath = Path.Combine(testPlatformPath, assemblyName.Name) + ".dll";
                    if(File.Exists(testPlatformFilePath))
                    {
                        assembly = Assembly.LoadFrom(testPlatformFilePath);
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
