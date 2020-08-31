// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// The test run configuration.
    /// </summary>
    internal class TestRunConfiguration : IXmlTestStore, IXmlTestStoreCustom
    {
        internal static readonly string DeploymentInDirectorySuffix = "In";

        #region  Fields
        private TestRunConfigurationId id;
        private readonly TrxFileHelper trxFileHelper;

        [StoreXmlSimpleField(DefaultValue = "")]
        private string name;

        private string runDeploymentRoot;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunConfiguration"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of Run Configuration.
        /// </param>
        /// <param name="trxFileHelper">
        /// InternalFileHelper instance to use in file operations.
        /// </param>
        internal TestRunConfiguration(string name, TrxFileHelper trxFileHelper)
        {
            EqtAssert.ParameterNotNull(name, "name");

            this.name = name;
            this.runDeploymentRoot = string.Empty;
            this.id = new TestRunConfigurationId();
            this.trxFileHelper = trxFileHelper;
        }

        #region IXmlTestStoreCustom Members

        /// <summary>
        /// Gets the element name.
        /// </summary>
        public string ElementName
        {
            get
            {
                return "TestSettings";
            }
        }

        /// <summary>
        /// Gets the namespace uri.
        /// </summary>
        public string NamespaceUri
        {
            get
            {
                return @"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            }
        }

        #endregion

        /// <summary>
        /// Gets directory that receives reverse-deployed files from Controller.
        /// </summary>
        public string RunDeploymentInDirectory
        {
            get
            {
                Debug.Assert(this.runDeploymentRoot != null, "runDeploymentRoot is null");
                return Path.Combine(this.runDeploymentRoot, DeploymentInDirectorySuffix);
            }
        }

        /// <summary>
        /// Gets or sets RunDeploymentRootDirectory
        /// INTERNAL PROPERTY. DO NOT USE (except execution).
        /// Run-level deployment root directory, already inside RunId directory, parent of In and Out directories.
        /// </summary>
        internal string RunDeploymentRootDirectory
        {
            get
            {
                return this.runDeploymentRoot;
            }

            set
            {
                Debug.Assert(!string.IsNullOrEmpty(value), "RunDeploymentRootDirectory.value should not be null or empty.");
                this.runDeploymentRoot = value;
            }
        }

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();

            // Save all fields marked as StoreXmlSimpleField.
            helper.SaveSingleFields(element, this, parameters);

            helper.SaveGuid(element, "@id", this.id.Id);

            // When saving and loading a TRX file, we want to use the run deployment root directory based on where the TRX file
            // is being saved to or loaded from
            object filePersistenceRootObjectType;
            if (parameters.TryGetValue(XmlFilePersistence.RootObjectType, out filePersistenceRootObjectType) &&
                (Type)filePersistenceRootObjectType == typeof(TestRun))
            {
                Debug.Assert(
                        parameters.ContainsKey(XmlFilePersistence.DirectoryPath),
                    "TestRun is the type of the root object being saved to a file, but the DirectoryPath was not specified in the XML test store parameters");

                Debug.Assert(
                        !string.IsNullOrEmpty(this.runDeploymentRoot),
                    "TestRun is the type of the root object being saved to a file, but the run deployment root directory is null or empty");

                // We are saving a TestRun object as the root element in a file (TRX file), so just save the test run directory
                // name (last directory in the run deployment root), which is the relative path to the run deployment root
                // directory from the directory where the TRX file exists
                helper.SaveSimpleField(
                        element,
                        "Deployment/@runDeploymentRoot",
                        trxFileHelper.MakePathRelative(this.runDeploymentRoot, Path.GetDirectoryName(this.runDeploymentRoot)),
                    string.Empty);
            }
            else
            {
                // We are not saving a TestRun object as the root element in a file (i.e., we're not saving a TRX file), so just
                // save the run deployment root directory as is
                helper.SaveSimpleField(element, "Deployment/@runDeploymentRoot", this.runDeploymentRoot, string.Empty);
            }
        }

        #endregion
    }
}
