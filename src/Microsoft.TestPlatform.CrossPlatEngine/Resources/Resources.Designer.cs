﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///    A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        internal Resources() {
        }
        
        /// <summary>
        ///    Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TestPlatform.CrossPlatEngine.Resources.Resources", typeof(Resources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///    Overrides the current thread's CurrentUICulture property for all
        ///    resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Exception occurred while instantiating discoverer : {0}.
        /// </summary>
        public static string DiscovererInstantiationException {
            get {
                return ResourceManager.GetString("DiscovererInstantiationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Multiple test adapters with the same uri &apos;{0}&apos; were found. Ignoring adapter &apos;{1}&apos;. Please uninstall the conflicting adapter(s) to avoid this warning..
        /// </summary>
        public static string DuplicateAdaptersFound {
            get {
                return ResourceManager.GetString("DuplicateAdaptersFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Ignoring the specified duplicate source &apos;{0}&apos;..
        /// </summary>
        public static string DuplicateSource {
            get {
                return ResourceManager.GetString("DuplicateSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to An exception occurred while test discoverer &apos;{0}&apos; was loading tests. Exception: {1}.
        /// </summary>
        public static string ExceptionFromLoadTests {
            get {
                return ResourceManager.GetString("ExceptionFromLoadTests", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to An exception occurred while invoking executor &apos;{0}&apos;: {1}.
        /// </summary>
        public static string ExceptionFromRunTests {
            get {
                return ResourceManager.GetString("ExceptionFromRunTests", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Could not find file {0}..
        /// </summary>
        public static string FileNotFound {
            get {
                return ResourceManager.GetString("FileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Host debugging is enabled. Please attach debugger to testhost process to continue..
        /// </summary>
        public static string HostDebuggerWarning {
            get {
                return ResourceManager.GetString("HostDebuggerWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Ignoring the test executor corresponding to test discoverer {0} because the discoverer does not have the DefaultExecutorUri attribute . You might need to re-install the test adapter add-in..
        /// </summary>
        public static string IgnoringExecutorAsNoDefaultExecutorUriAttribute {
            get {
                return ResourceManager.GetString("IgnoringExecutorAsNoDefaultExecutorUriAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Failed to initialize client proxy: could not connect to test process..
        /// </summary>
        public static string InitializationFailed {
            get {
                return ResourceManager.GetString("InitializationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to This operation is not allowed in the context of a non-debug run..
        /// </summary>
        public static string LaunchDebugProcessNotAllowedForANonDebugRun {
            get {
                return ResourceManager.GetString("LaunchDebugProcessNotAllowedForANonDebugRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to No test discoverer is registered to perform discovery of test cases. Register a test discoverer and try again..
        /// </summary>
        public static string NoDiscovererRegistered {
            get {
                return ResourceManager.GetString("NoDiscovererRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Could not find {0}. Make sure that the dotnet is installed on the machine..
        /// </summary>
        public static string NoDotnetExeFound {
            get {
                return ResourceManager.GetString("NoDotnetExeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Could not find test executor with URI &apos;{0}&apos;.  Make sure that the test executor is installed and supports .net runtime version {1}..
        /// </summary>
        public static string NoMatchingExecutor {
            get {
                return ResourceManager.GetString("NoMatchingExecutor", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Could not find testhost.dll for source &apos;{0}&apos;. Make sure test project has a nuget reference of package &quot;microsoft.testplatform.testhost&quot;..
        /// </summary>
        public static string NoTestHostFileExist {
            get {
                return ResourceManager.GetString("NoTestHostFileExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to None of the specified source(s) &apos;{0}&apos; is valid. Fix the above errors/warnings and then try again. .
        /// </summary>
        public static string NoValidSourceFoundForDiscovery {
            get {
                return ResourceManager.GetString("NoValidSourceFoundForDiscovery", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to , .
        /// </summary>
        public static string StringSeperator {
            get {
                return ResourceManager.GetString("StringSeperator", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to No test is available in {0}. Make sure that installed test discoverers &amp; executors, platform &amp; framework version settings are appropriate and try again..
        /// </summary>
        public static string TestRunFailed_NoTestsAreAvailableInTheSources {
            get {
                return ResourceManager.GetString("TestRunFailed_NoTestsAreAvailableInTheSources", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to No tests matched the filter because it contains one or more properties that are not valid ({0}). Specify filter expression containing valid properties ({1}) and try again..
        /// </summary>
        public static string UnsupportedPropertiesInTestCaseFilter {
            get {
                return ResourceManager.GetString("UnsupportedPropertiesInTestCaseFilter", resourceCulture);
            }
        }
    }
}
