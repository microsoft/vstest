// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Session/Run configuration information for data collection.
    /// </summary>
    internal class DataCollectionSessionConfiguration
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionSessionConfiguration"/> class. 
        /// </summary>
        /// <param name="id">
        /// Session id
        /// </param>
        /// <param name="outputDirectory">
        /// Base output directory to store collection logs for this session.
        /// </param>
        internal DataCollectionSessionConfiguration(SessionId id, string outputDirectory)
        {
            ValidateArg.NotNullOrEmpty(outputDirectory, "outputDirectory");
            this.SessionId = id;
            this.OutputDirectory = outputDirectory;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets Id of associated session 
        /// </summary>
        internal SessionId SessionId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets base output directory to store collection logs.
        /// </summary>
        internal string OutputDirectory
        {
            get;
            private set;
        }

        #endregion
    }
}
