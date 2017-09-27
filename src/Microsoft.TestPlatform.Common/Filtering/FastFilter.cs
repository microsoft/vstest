// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal sealed class FastFilter
    {

        private readonly string filterPropertyName;
        private readonly HashSet<string> filterPropertyValues;

        private bool IsFilteredOutWhenMatched { get; }

        internal Regex PropertyValueRegex { get; set; }

        internal FastFilter(string filterPropertyName, HashSet<string> filterPropertyValues, Operation filterOperation, Operator filterOperator)
        {
            ValidateArg.NotNullOrEmpty(filterPropertyName, nameof(filterPropertyName));
            ValidateArg.NotNull(filterPropertyValues, nameof(filterPropertyValues));
            
            this.filterPropertyName = filterPropertyName;
            this.filterPropertyValues = filterPropertyValues;

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
            return properties.Contains(this.filterPropertyName, StringComparer.OrdinalIgnoreCase)
                ? null
                : new[] { this.filterPropertyName };
        }

        internal bool Evaluate(Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");

            if (!TryGetSinglePropertyValue(this.filterPropertyName, propertyValueProvider, out var value))
            {
                return false;
            }

            if (PropertyValueRegex != null)
            {
                var match = PropertyValueRegex.Match(value);
                if (match.Success)
                {
                    value = match.Value;
                }
                else
                {
                    return false;
                }
            }

            var matched = this.filterPropertyValues.Contains(value);
            return IsFilteredOutWhenMatched ? !matched : matched;
        }

        private static bool TryGetSinglePropertyValue(string name, Func<string, Object> propertyValueProvider, out string singleValue)
        {
            singleValue = propertyValueProvider(name) as string;
            return singleValue != null;
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

            private HashSet<string> filterHashSet = new HashSet<string>();

            private bool containsValidFilter = true;

            internal bool ContainsValidFilter => containsValidFilter && conditionEncountered;

            internal void AddOperator(Operator @operator)
            {
                if (!containsValidFilter || !(@operator == Operator.And || @operator == Operator.Or))
                {
                    return;
                }

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
                        filterHashSet.Add(condition.Value);
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

                    filterHashSet.Add(condition.Value);

                    // Don't support `Contains`.
                    if (fastFilterOperation != Operation.Equal && fastFilterOperation != Operation.NotEqual)
                    {
                        containsValidFilter = false;
                    }
                }
            }

            internal FastFilter ToFastFilter()
            {
                return ContainsValidFilter ? new FastFilter(filterPropertyName, filterHashSet, fastFilterOperation, fastFilterOperator) : null;
            }
        }
    }
}
