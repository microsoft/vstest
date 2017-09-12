// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

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
                    var invalidPropertiesString = string.Join(CrossPlatEngineResources.StringSeperator, invalidProperties);
                    var validPropertiesSttring = supportedProperties == null ? string.Empty : string.Join(CrossPlatEngineResources.StringSeperator, supportedProperties.ToArray());
                    var errorMessage = string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.UnsupportedPropertiesInTestCaseFilter, invalidPropertiesString, validPropertiesSttring);

                    // For unsupported property don’t throw exception, just log the message. Later it is going to handle properly with TestCaseFilterExpression.MatchTestCase().
                    EqtTrace.Info(errorMessage);
                }
            }

            return adapterSpecificTestCaseFilter;
        }
    }
}
