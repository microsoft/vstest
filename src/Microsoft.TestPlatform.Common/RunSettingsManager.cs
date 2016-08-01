// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Manages the active run settings.
    /// </summary>
    internal class RunSettingsManager : IRunSettingsProvider
    {
        #region private members

        private static object lockObject = new object();

        private static RunSettingsManager runSettingsManagerInstance;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        private RunSettingsManager()
        {
            this.ActiveRunSettings = new RunSettings();
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
                if (runSettingsManagerInstance != null)
                {
                    return runSettingsManagerInstance;
                }

                lock (lockObject)
                {
                    if (runSettingsManagerInstance == null)
                    {
                        runSettingsManagerInstance = new RunSettingsManager();
                    }
                }

                return runSettingsManagerInstance;
            }
            internal set
            {
                runSettingsManagerInstance = value;
            }
        }

        /// <summary>
        /// Set the active run settings.
        /// </summary>
        /// <param name="runSettings">RunSettings to make the active Run Settings.</param>
        public void SetActiveRunSettings(RunSettings runSettings)
        {
            ValidateArg.NotNull<RunSettings>(runSettings, "runSettings");
            this.ActiveRunSettings = runSettings;
        }

        #endregion
    }
}
