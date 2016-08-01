// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// The in procedure data collection utilities.
    /// </summary>
    public static class InProcDataCollectionUtilities
    {
        /// <summary>
        /// Gets the in process data collection run settings.
        /// </summary>
        public static DataCollectionRunSettings InProcDataCollectionRunSettings { get; private set; }


        public static bool InProcDataCollectionEnabled
        {
            get
            {
                var isEnabled = InProcDataCollectionRunSettings?.IsCollectionEnabled ?? false;
                return isEnabled && InProcDataCollectionRunSettings.DataCollectorSettingsList.Count > 0;
            }
        }

        /// <summary>
        /// The read in process data collection run settings.
        /// </summary>
        /// <param name="runSettings">
        /// The run settings.
        /// </param>
        public static void ReadInProcDataCollectionRunSettings(string runSettings)
        {
            InProcDataCollectionRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(runSettings);
        }

        /// <summary>
        /// The get data collector settings.
        /// </summary>
        /// <returns>
        /// The <see cref="InProcDataCollectorSettings"/>.
        /// </returns>
        public static Collection<DataCollectorSettings> GetInProcDataCollectorSettings()
        {
            if (InProcDataCollectionEnabled)
            {
                return InProcDataCollectionRunSettings.DataCollectorSettingsList;
            }

            return null;
        }
    }
}
