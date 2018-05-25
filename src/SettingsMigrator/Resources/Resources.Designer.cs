﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.TestPlatform.SettingsMigrator.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SettingsMigrator.Resources.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Any LegacySettings node already present in the runsettings will be removed..
        /// </summary>
        internal static string IgnoringLegacySettings {
            get {
                return ResourceManager.GetString("IgnoringLegacySettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to RunSettings does not contain an embedded testSettings, not migrating..
        /// </summary>
        internal static string NoEmbeddedSettings {
            get {
                return ResourceManager.GetString("NoEmbeddedSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The migrated RunSettings file has been created at: {0}.
        /// </summary>
        internal static string RunSettingsCreated {
            get {
                return ResourceManager.GetString("RunSettingsCreated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The attributes agentNotRespondingTimeout, deploymentTimeout, scriptTimeout are not supported, so these will not be migrated..
        /// </summary>
        internal static string UnsupportedAttributes {
            get {
                return ResourceManager.GetString("UnsupportedAttributes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Valid usage: SettingsMigrator.exe &lt;Full path to testsettings file or runsettings file to be migrated&gt; &lt;Full path to new runsettings file&gt;
        ///Example: SettingsMigrator.exe  E:\MyTest\MyTestSettings.testsettings  E:\MyTest\MyRunSettings.runsettings
        ///Example: SettingsMigrator.exe  E:\MyTest\MyOldRunSettings.runsettings  E:\MyTest\MyRunSettings.runsettings.
        /// </summary>
        internal static string ValidUsage {
            get {
                return ResourceManager.GetString("ValidUsage", resourceCulture);
            }
        }
    }
}
