// Copyright (c) Microsoft. All rights reserved.


namespace Microsoft.TestPlatform.Extensions.TrxLogger.XML
{
    internal class XmlFilePersistence: XmlPersistence
    {
        #region Constants

        /// <summary>
        /// Type of the object that is persisted to the file, the object that represents the root element
        /// </summary>
        public const string RootObjectType = "RootObjectType";

        /// <summary>
        /// The directory to where the file is being saved, or from where the file is being loaded
        /// </summary>
        public const string DirectoryPath = "DirectoryPath";

        #endregion
    }
}
