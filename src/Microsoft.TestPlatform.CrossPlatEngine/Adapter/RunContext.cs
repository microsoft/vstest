// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Provides user specified runSettings and framework provided context of the run. 
    /// </summary>
    public class RunContext : DiscoveryContext, IRunContext
    {
        /// <summary>
        /// Gets a value indicating whether the execution process should be kept alive after the run is finished.
        /// </summary>
        public bool KeepAlive { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the discovery or execution is happening in In-process or out-of-process.
        /// </summary>
        public bool InIsolation { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether data collection is enabled.
        /// </summary>
        public bool IsDataCollectionEnabled { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the test is being debugged. 
        /// </summary>
        public bool IsBeingDebugged { get; internal set; }

        /// <summary>
        /// Gets the directory which should be used for storing result files/deployment files etc.
        /// </summary>
        public string TestRunDirectory { get; internal set; }

        /// <summary>
        /// Gets the directory for Solution.
        /// </summary>
        public string SolutionDirectory { get; internal set; }

        /// <summary>
        /// Gets or sets the FilterExpressionWrapper instance as created from filter string.
        /// </summary>
        internal FilterExpressionWrapper FilterExpressionWrapper
        {
            get;
            set;
        }

        /// <summary>
        /// Returns TestCaseFilterExpression validated for supportedProperties.
        /// If there is a parsing error or filter expression has unsupported properties, TestPlatformFormatException() is thrown.
        /// </summary>
        /// <param name="supportedProperties"> The supported Properties. </param>
        /// <param name="propertyProvider"> The property Provider. </param>
        /// <returns> The <see cref="ITestCaseFilterExpression"/>. </returns>
        public ITestCaseFilterExpression GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            TestCaseFilterExpression adapterSpecificTestCaseFilter = null;
            if (this.FilterExpressionWrapper != null)
            {
                if (!string.IsNullOrEmpty(this.FilterExpressionWrapper.ParseError))
                {
                    throw new TestPlatformFormatException(this.FilterExpressionWrapper.ParseError, this.FilterExpressionWrapper.FilterString);
                }

                adapterSpecificTestCaseFilter = new TestCaseFilterExpression(this.FilterExpressionWrapper);
                var invalidProperties = adapterSpecificTestCaseFilter.ValidForProperties(supportedProperties, propertyProvider);

                if (invalidProperties != null)
                {
                    var invalidPropertiesString = string.Join(CrossPlatEngine.Resources.StringSeperator, invalidProperties);
                    var validPropertiesSttring = supportedProperties == null ? string.Empty : string.Join(CrossPlatEngine.Resources.StringSeperator, supportedProperties.ToArray());
                    var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngine.Resources.UnsupportedPropertiesInTestCaseFilter, invalidPropertiesString, validPropertiesSttring);
                    throw new TestPlatformFormatException(errorMessage, this.FilterExpressionWrapper.FilterString);
                }
            }

            return adapterSpecificTestCaseFilter;
        }
    }
}
