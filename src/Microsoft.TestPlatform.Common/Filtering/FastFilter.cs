// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal sealed class FastFilter
    {
        internal string FilterPropertyName { get; }

        internal ImmutableHashSet<string> FilterPropertyValues { get; }

        internal bool IsFilteredOutWhenMatched { get; }

        internal Regex PropertyValueRegex { get; set; }

        internal string PropertyValueRegexReplacement { get; set; }

        internal FastFilter(string filterPropertyName, ImmutableHashSet<string> filterPropertyValues, Operation filterOperation, Operator filterOperator)
        {
            ValidateArg.NotNullOrEmpty(filterPropertyName, nameof(filterPropertyName));
            ValidateArg.NotNull(filterPropertyValues, nameof(filterPropertyValues));
            
            this.FilterPropertyName = filterPropertyName;
            this.FilterPropertyValues = filterPropertyValues;

            if (filterOperation == Operation.Equal && (filterOperator == Operator.Or || filterOperator == Operator.None))
            {
                IsFilteredOutWhenMatched = false;
            }
            else if (filterOperation == Operation.NotEqual && (filterOperator == Operator.And || filterOperator == Operator.None))
            {
                IsFilteredOutWhenMatched = true;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        internal string[] ValidForProperties(IEnumerable<string> properties)
        {
            return properties.Contains(this.FilterPropertyName, StringComparer.OrdinalIgnoreCase)
                ? null
                : new[] { this.FilterPropertyName };
        }

        internal bool Evaluate(Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");

            if (!TryGetPropertyValue(this.FilterPropertyName, propertyValueProvider, out var singleValue, out var multiValues))
            {
                // No value corresponding to given name, treat it as unmatched.
                return IsFilteredOutWhenMatched;
            }

            bool matched;
            if (singleValue != null)
            {
                var value = PropertyValueRegex == null ? singleValue : ApplyRegex(singleValue);
                matched = value != null && this.FilterPropertyValues.Contains(value.ToLowerInvariant());
            }
            else
            {
                matched = (PropertyValueRegex == null ? multiValues : multiValues.Select(value => ApplyRegex(value)))
                    .Any(result => result != null && this.FilterPropertyValues.Contains(result.ToLowerInvariant()));
            }

            return IsFilteredOutWhenMatched ? !matched : matched;
        }

        /// <summary>
        /// Apply regex matching or replacement to given value.
        /// </summary>
        /// <returns>For matching, returns the result of matching, null if no match found. For replacement, returns the result of replacement.</returns>
        private string ApplyRegex(string value)
        {
            Debug.Assert(PropertyValueRegex != null);

            string result = null;
            if (PropertyValueRegexReplacement == null)
            {
                var match = PropertyValueRegex.Match(value);
                if (match.Success)
                {
                    result = match.Value;
                }
            }
            else
            {
                result = PropertyValueRegex.Replace(value, PropertyValueRegexReplacement);
            }
            return result;
        }

        /// <summary>
        /// Returns property value for Property using propertValueProvider.
        /// </summary>
        private static bool TryGetPropertyValue(string name, Func<string, Object> propertyValueProvider, out string singleValue, out string[] multiValues)
        {
            var propertyValue = propertyValueProvider(name);
            if (null != propertyValue)
            {
                multiValues = propertyValue as string[];
                singleValue = multiValues == null? propertyValue.ToString() : null;
                return true;
            }

            singleValue = null;
            multiValues = null;
            return false;
        }

        internal static Builder CreateBuilder()
        {
            return new Builder();
        }

        internal sealed class Builder
        {
            private bool operatorEncountered = false;
            private Operator fastFilterOperator = Operator.None;

            private bool conditionEncountered = false;
            private Operation fastFilterOperation;
            private string filterPropertyName;
            private ImmutableHashSet<string>.Builder filterHashSetBuilder = ImmutableHashSet.CreateBuilder<string>();

            private bool containsValidFilter = true;

            internal bool ContainsValidFilter => containsValidFilter && conditionEncountered;

            internal void AddOperator(Operator @operator)
            {
                if (containsValidFilter && (@operator == Operator.And || @operator == Operator.Or))
                {
                    if (operatorEncountered)
                    {
                        containsValidFilter = fastFilterOperator == @operator;
                    }
                    else
                    {
                        operatorEncountered = true;
                        fastFilterOperator = @operator;
                    }
                }
                else
                {
                    containsValidFilter = false;
                }
            }

            internal void AddCondition(Condition condition)
            {
                if (!containsValidFilter)
                {
                    return;
                }

                if (conditionEncountered)
                {
                    if (condition.Operation == fastFilterOperation && condition.Name.Equals(filterPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        filterHashSetBuilder.Add(condition.Value.ToLowerInvariant());
                    }
                    else
                    {
                        containsValidFilter = false;
                    }
                }
                else
                {
                    conditionEncountered = true;
                    fastFilterOperation = condition.Operation;
                    filterPropertyName = condition.Name;

                    filterHashSetBuilder.Add(condition.Value.ToLowerInvariant());

                    // Don't support `Contains`.
                    if (fastFilterOperation != Operation.Equal && fastFilterOperation != Operation.NotEqual)
                    {
                        containsValidFilter = false;
                    }
                }
            }

            internal FastFilter ToFastFilter()
            {
                return ContainsValidFilter ? new FastFilter(filterPropertyName, filterHashSetBuilder.ToImmutable(), fastFilterOperation, fastFilterOperator) : null;
            }
        }
    }
}
