// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering;

internal enum Operation
{
    Equal,
    NotEqual,
    Contains,
    NotContains
}

/// <summary>
/// Operator in order of precedence.
/// Precedence(And) > Precedence(Or)
/// Precedence of OpenBrace and CloseBrace operators is not used, instead parsing code takes care of same.
/// </summary>
internal enum Operator
{
    None,
    Or,
    And,
    OpenBrace,
    CloseBrace,
}

/// <summary>
/// Represents a condition in filter expression.
/// </summary>
internal class Condition
{
    /// <summary>
    ///  Default property name which will be used when filter has only property value.
    /// </summary>
    public const string DefaultPropertyName = "FullyQualifiedName";

    /// <summary>
    ///  Default operation which will be used when filter has only property value.
    /// </summary>
    public const Operation DefaultOperation = Operation.Contains;

    /// <summary>
    /// Name of the property used in condition.
    /// </summary>
    internal string Name
    {
        get;
        private set;
    }

    /// <summary>
    /// Value for the property.
    /// </summary>
    internal string Value
    {
        get;
        private set;
    }

    /// <summary>
    /// Operation to be performed.
    /// </summary>
    internal Operation Operation
    {
        get;
        private set;
    }
    internal Condition(string name, Operation operation, string value)
    {
        Name = name;
        Operation = operation;
        Value = value;
    }

    /// <summary>
    /// Evaluate this condition for testObject.
    /// </summary>
    internal bool Evaluate(Func<string, object?> propertyValueProvider)
    {
        ValidateArg.NotNull(propertyValueProvider, nameof(propertyValueProvider));
        var result = false;
        var multiValue = GetPropertyValue(propertyValueProvider);
        switch (Operation)
        {
            case Operation.Equal:
                // if any value in multi-valued property matches 'this.Value', for Equal to evaluate true.
                if (null != multiValue)
                {
                    foreach (string propertyValue in multiValue)
                    {
                        result = result || string.Equals(propertyValue, Value, StringComparison.OrdinalIgnoreCase);
                        if (result)
                        {
                            break;
                        }
                    }
                }
                break;


            case Operation.NotEqual:
                // all values in multi-valued property should not match 'this.Value' for NotEqual to evaluate true.
                result = true;

                // if value is null.
                if (null != multiValue)
                {
                    foreach (string propertyValue in multiValue)
                    {
                        result = result && !string.Equals(propertyValue, Value, StringComparison.OrdinalIgnoreCase);
                        if (!result)
                        {
                            break;
                        }
                    }
                }
                break;

            case Operation.Contains:
                // if any value in multi-valued property contains 'this.Value' for 'Contains' to be true.
                if (null != multiValue)
                {
                    foreach (string propertyValue in multiValue)
                    {
                        TPDebug.Assert(null != propertyValue, "PropertyValue can not be null.");
                        result = result || propertyValue.IndexOf(Value, StringComparison.OrdinalIgnoreCase) != -1;
                        if (result)
                        {
                            break;
                        }
                    }
                }
                break;

            case Operation.NotContains:
                // all values in multi-valued property should not contain 'this.Value' for NotContains to evaluate true.
                result = true;

                if (null != multiValue)
                {
                    foreach (string propertyValue in multiValue)
                    {
                        TPDebug.Assert(null != propertyValue, "PropertyValue can not be null.");
                        result = result && propertyValue.IndexOf(Value, StringComparison.OrdinalIgnoreCase) == -1;
                        if (!result)
                        {
                            break;
                        }
                    }
                }
                break;
        }
        return result;
    }

    /// <summary>
    /// Returns a condition object after parsing input string of format '<propertyName>Operation<propertyValue>'
    /// </summary>
    internal static Condition Parse(string? conditionString)
    {
        if (conditionString.IsNullOrWhiteSpace())
        {
            ThrownFormatExceptionForInvalidCondition(conditionString);
        }

        var parts = TokenizeFilterConditionString(conditionString).ToArray();
        if (parts.Length == 1)
        {
            // If only parameter values is passed, create condition with default property name,
            // default operation and given condition string as parameter value.
            return new Condition(DefaultPropertyName, DefaultOperation, FilterHelper.Unescape(conditionString.Trim()));
        }

        if (parts.Length != 3)
        {
            ThrownFormatExceptionForInvalidCondition(conditionString);
        }

        for (int index = 0; index < 3; index++)
        {
            if (parts[index].IsNullOrWhiteSpace())
            {
                ThrownFormatExceptionForInvalidCondition(conditionString);
            }
            parts[index] = parts[index].Trim();
        }

        Operation operation = GetOperator(parts[1]);
        Condition condition = new(parts[0], operation, FilterHelper.Unescape(parts[2]));
        return condition;
    }

    [DoesNotReturn]
    private static void ThrownFormatExceptionForInvalidCondition(string? conditionString)
    {
        throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException,
            string.Format(CultureInfo.CurrentCulture, CommonResources.InvalidCondition, conditionString)));
    }

    /// <summary>
    /// Check if condition validates any property in properties.
    /// </summary>
    internal bool ValidForProperties(IEnumerable<string> properties, Func<string, TestProperty?>? propertyProvider)
    {
        bool valid = false;

        if (properties.Contains(Name, StringComparer.OrdinalIgnoreCase))
        {
            valid = true;

            // Check if operation ~ (Contains) is on property of type string.
            if (Operation == Operation.Contains)
            {
                valid = ValidForContainsOperation(propertyProvider);
            }
        }
        return valid;
    }

    private bool ValidForContainsOperation(Func<string, TestProperty?>? propertyProvider)
    {
        bool valid = true;

        // It is OK for propertyProvider to be null, no syntax check will happen.

        // Check validity of operator only if related TestProperty is non-null.
        // if null, it might be custom validation ignore it.
        if (null != propertyProvider)
        {
            TestProperty? testProperty = propertyProvider(Name);
            if (null != testProperty)
            {
                Type propertyType = testProperty.GetValueType();
                valid = typeof(string) == propertyType ||
                        typeof(string[]) == propertyType;
            }
        }
        return valid;
    }

    /// <summary>
    /// Return Operation corresponding to the operationString
    /// </summary>
    private static Operation GetOperator(string operationString)
    {
        return operationString switch
        {
            "=" => Operation.Equal,
            "!=" => Operation.NotEqual,
            "~" => Operation.Contains,
            "!~" => Operation.NotContains,
            _ => throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, string.Format(CultureInfo.CurrentCulture, CommonResources.InvalidOperator, operationString))),
        };
    }

    /// <summary>
    /// Returns property value for Property using propertValueProvider.
    /// </summary>
    private string[]? GetPropertyValue(Func<string, object?> propertyValueProvider)
    {
        var propertyValue = propertyValueProvider(Name);
        if (null != propertyValue)
        {
            if (propertyValue is not string[] multiValue)
            {
                multiValue = new string[1];
                multiValue[0] = propertyValue.ToString()!;
            }
            return multiValue;
        }

        return null;
    }

    internal static IEnumerable<string> TokenizeFilterConditionString(string str)
    {
        return str == null ? throw new ArgumentNullException(nameof(str)) : TokenizeFilterConditionStringWorker(str);

        static IEnumerable<string> TokenizeFilterConditionStringWorker(string s)
        {
            StringBuilder tokenBuilder = new();

            var last = '\0';
            for (int i = 0; i < s.Length; ++i)
            {
                var current = s[i];

                if (last == FilterHelper.EscapeCharacter)
                {
                    // Don't check if `current` is one of the special characters here.
                    // Instead, we blindly let any character follows '\' pass though and
                    // relies on `FilterHelpers.Unescape` to report such errors.
                    tokenBuilder.Append(current);

                    if (current == FilterHelper.EscapeCharacter)
                    {
                        // We just encountered double backslash (i.e. escaped '\'), therefore set `last` to '\0'
                        // so the second '\' (i.e. current) will not be treated as the prefix of escape sequence
                        // in next iteration.
                        current = '\0';
                    }
                }
                else
                {
                    switch (current)
                    {
                        case '=':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            yield return "=";
                            break;

                        case '!':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            // Determine if this is a "!=" or "!~" or just a single "!".
                            var next = i + 1;
                            if (next < s.Length && s[next] == '=')
                            {
                                i = next;
                                current = '=';
                                yield return "!=";
                            }
                            else if (next < s.Length && s[next] == '~')
                            {
                                i = next;
                                current = '~';
                                yield return "!~";
                            }
                            else
                            {
                                yield return "!";
                            }
                            break;

                        case '~':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            yield return "~";
                            break;

                        default:
                            tokenBuilder.Append(current);
                            break;
                    }
                }
                last = current;
            }

            if (tokenBuilder.Length > 0)
            {
                yield return tokenBuilder.ToString();
            }
        }
    }
}
