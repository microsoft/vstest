// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.Common;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using TestElement = Microsoft.VisualStudio.TestTools.Common.TestElement;
    using TestType = Microsoft.VisualStudio.TestTools.Common.TestType;

    /// <summary>
    /// The test platform test element.
    /// </summary>
    [Guid("2955A1D9-C1B3-4ADF-A183-B0098438C8F5")]
#if NET461
    [Serializable]
#endif
    internal class TestPlatformTestElement : TestElement
    {
        #region Fields

        private static TestType testType = new TestType(typeof(TestPlatformTestElement).GUID);

        // The name of the adapter - this is used by Agent to figure out which Adapter to use.
        private const string adapterName = null;

        #endregion

        #region Constructors

        public TestPlatformTestElement(TestCase testCase)
            : base(new TestId(testCase.Id), testCase.FullyQualifiedName, testCase.DisplayName, testCase.Source)
        {
            this.m_executionId = new TestExecId(testCase.Id);
        }



        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="copy"></param>
        protected TestPlatformTestElement(TestPlatformTestElement copy)
            : base(copy)
        {
        }

        #endregion

        #region TestElement Methods

        /// <summary>
        /// Readonly. Exception will be thrown if trying to set it.
        /// Obsolete.
        /// </summary>
        // Make it readonly so that we don't load this property
        [ReadOnly(true)]
        public override bool ReadOnly
        {
            get
            {
                return false;
            }
            set
            {
                // should not set this property
                throw new NotSupportedException("Read only property");
            }
        }


        public override TestType TestType
        {
            get { return testType; }
        }


        public override object Clone()
        {
            return new TestPlatformTestElement(this);
        }


        /// <summary>
        /// Adapter for test.
        /// </summary>
        public override string Adapter
        {
            get
            {
                return adapterName;
            }
        }

        /// <summary>
        /// No plugin for test.
        /// </summary>
        public override string ControllerPlugin
        {
            get { return null; }
        }

        public override bool CanBeAggregated
        {
            get { return true; }
        }

        /// <summary>
        /// TestPlatform tests are automated.
        /// </summary>
        public override bool IsAutomated { get { return true; } }

        #endregion
    }
}
