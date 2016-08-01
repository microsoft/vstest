// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    /// <summary>
    /// Attribute applied to ISettingsProviders to associate it with a settings
    /// name.  This name will be used to request the settings from the RunSettings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SettingsNameAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Initializes with the name of the settings.
        /// </summary>
        /// <param name="settingsName">Name of the settings</param>
        public SettingsNameAttribute(string settingsName)
        {
            if (string.IsNullOrWhiteSpace(settingsName))
            {
                throw new ArgumentException(CommonResources.CannotBeNullOrEmpty, "settingsName");
            }
            
            SettingsName = settingsName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The name of the settings.
        /// </summary>
        public string SettingsName { get; private set; }
        
        #endregion
    }
}
