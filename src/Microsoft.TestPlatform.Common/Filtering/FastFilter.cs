// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal sealed class FastFilter
    {
        internal ImmutableDictionary<string, ISet<string>> FilterProperties { get; }

        internal bool IsFilteredOutWhenMatched { get; }

        internal Regex PropertyValueRegex { get; set; }

        internal string PropertyValueRegexReplacement { get; set; }

        internal FastFilter(ImmutableDictionary<string, ISet<string>> filterProperties, Operation filterOperation, Operator filterOperator)
        {
            ValidateArg.NotNullOrEmpty(filterProperties, nameof(filterProperties));

            this.FilterProperties = filterProperties;

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
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.FastFilterException));
            }
        }

        internal string[] ValidForProperties(IEnumerable<string> properties)
        {
            return this.FilterProperties.Keys.All(name => properties.Contains(name))
                ? null
                : this.FilterProperties.Keys.Where(name => !properties.Contains(name)).ToArray();
        }

        internal bool Evaluate(Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");

            bool matched = false;
            foreach (var name in this.FilterProperties.Keys)
            {
                // If there is no value corresponding to given name, treat it as unmatched.
                if (TryGetPropertyValue(name, propertyValueProvider, out var singleValue, out var multiValues))
                {
                    if (singleValue != null)
                    {
                        var value = PropertyValueRegex == null ? singleValue : ApplyRegex(singleValue);
                        matched = value != null && this.FilterProperties[name].Contains(value);
                    }
                    else
                    {
                        matched = (PropertyValueRegex == null ? multiValues : multiValues.Select(value => ApplyRegex(value)))
                            .Any(result => result != null && this.FilterProperties[name].Contains(result));
                    }

                    if (matched)
                    {
                        break;
                    }
                }
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
                singleValue = multiValues == null ? propertyValue.ToString() : null;
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
            private ImmutableDictionary<string, ImmutableHashSet<string>.Builder>.Builder filterDictionaryBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>.Builder>(StringComparer.OrdinalIgnoreCase);

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
                        if ((fastFilterOperation == Operation.NotEqual && fastFilterOperator == Operator.Or)
                            || (fastFilterOperation == Operation.Equal && fastFilterOperator == Operator.And))
                        {
                            containsValidFilter = false;
                        }
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
                    if (condition.Operation == fastFilterOperation)
                    {
                        AddProperty(condition.Name, condition.Value);
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
                    AddProperty(condition.Name, condition.Value);

                    // Don't support `Contains`.
                    if (fastFilterOperation != Operation.Equal && fastFilterOperation != Operation.NotEqual)
                    {
                        containsValidFilter = false;
                    }
                }
            }

            private void AddProperty(string name, string value)
            {
                if (!filterDictionaryBuilder.TryGetValue(name, out var values))
                {
                    values = ImmutableHashSet.CreateBuilder(StringComparer.OrdinalIgnoreCase);
                    filterDictionaryBuilder.Add(name, values);
                }

                values.Add(value);
            }

            internal FastFilter ToFastFilter()
            {
                if (ContainsValidFilter)
                {
                    return new FastFilter(
                        filterDictionaryBuilder.ToImmutableDictionary(kvp => kvp.Key, kvp => (ISet<string>)filterDictionaryBuilder[kvp.Key].ToImmutable()),
                        fastFilterOperation,
                        fastFilterOperator);
                }

                return null;
            }
        }
    }
}
