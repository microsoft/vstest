// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Class holds information related to filtering criteria.
    /// </summary>
    public class FilterExpressionWrapper
    {
        /// <summary>
        /// FilterExpression corresponding to filter criteria
        /// </summary>
        private FilterExpression filterExpression;
        
        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString.  
        /// </summary>
        public FilterExpressionWrapper(string filterString)
        {
            ValidateArg.NotNullOrEmpty(filterString, "filterString");

            this.FilterString = filterString;
            try
            {
                this.filterExpression = FilterExpression.Parse(filterString);
            }
            catch (FormatException ex)
            {
                this.ParseError = ex.Message;
            }
        }
        
        /// <summary>
        /// User specified filter criteria.
        /// </summary>
        public string FilterString
        {
            get;
            private set;
        }

        /// <summary>
        /// Parsing error (if any), when parsing 'FilterString' with built-in parser.
        /// </summary>
        public string ParseError
        {
            get;
            private set;
        }

        /// <summary>
        /// Validate if underlying filter expression is valid for given set of supported properties.
        /// </summary>
        public string[] ValidForProperties(IEnumerable<String> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            string[] invalidProperties = null;
            if (null != this.filterExpression)
            {
                invalidProperties = this.filterExpression.ValidForProperties(supportedProperties, propertyProvider);
            }
            return invalidProperties;
        }
        
        /// <summary>
        /// Evaluate filterExpression with given propertyValueProvider.
        /// </summary>
        public bool Evaluate(Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");
            if (null == this.filterExpression)
            {
                return false;
            }
            return this.filterExpression.Evaluate(propertyValueProvider);
        }

    }
}
