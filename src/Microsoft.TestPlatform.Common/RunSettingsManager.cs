// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common;

using Interfaces;

using ObjectModel;

/// <summary>
/// Manages the active run settings.
/// </summary>
internal class RunSettingsManager : IRunSettingsProvider
{
    #region private members

    private static readonly object LockObject = new();

    private static RunSettingsManager s_runSettingsManagerInstance;

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    private RunSettingsManager()
    {
        ActiveRunSettings = new RunSettings();
    }


    #endregion

    #region IRunSettingsProvider

    /// <summary>
    /// Gets the active run settings.
    /// </summary>
    public RunSettings ActiveRunSettings { get; private set; }

    #endregion

    #region Public Methods

    public static RunSettingsManager Instance
    {
        get
        {
            if (s_runSettingsManagerInstance != null)
            {
                return s_runSettingsManagerInstance;
            }

            lock (LockObject)
            {
                if (s_runSettingsManagerInstance == null)
                {
                    s_runSettingsManagerInstance = new RunSettingsManager();
                }
            }

            return s_runSettingsManagerInstance;
        }
        internal set
        {
            s_runSettingsManagerInstance = value;
        }
    }

    /// <summary>
    /// Set the active run settings.
    /// </summary>
    /// <param name="runSettings">RunSettings to make the active Run Settings.</param>
    public void SetActiveRunSettings(RunSettings runSettings)
    {
        ValidateArg.NotNull(runSettings, nameof(runSettings));
        ActiveRunSettings = runSettings;
    }

    #endregion
}
