// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

    /// <summary>
    /// Represents an expression tree.
    /// Supports:
    ///     Logical Operators:  &, |
    ///     Equality Operators: =, !=
    ///     Parenthesis (, ) for grouping.
    /// </summary>
    internal class FilterExpression
    {
        /// <summary>
        /// Condition, if expression is conditional expression.
        /// </summary>
        private readonly Condition condition;

        /// <summary>
        /// Left operand, when expression is logical expression.
        /// </summary>
        private readonly FilterExpression left;

        /// <summary>
        /// Right operand, when expression is logical expression.
        /// </summary>
        private readonly FilterExpression right;

        /// <summary>
        /// If logical expression is using logical And ('&') operator.
        /// </summary>
        private readonly bool areJoinedByAnd;

        #region Constructors

        private FilterExpression(FilterExpression left, FilterExpression right, bool areJoinedByAnd)
        {
            ValidateArg.NotNull(left, nameof(left));
            ValidateArg.NotNull(right, nameof(right));

            this.left = left;
            this.right = right;
            this.areJoinedByAnd = areJoinedByAnd;
        }

        private FilterExpression(Condition condition)
        {
            ValidateArg.NotNull(condition, nameof(condition));
            this.condition = condition;
        }
        #endregion

        /// <summary>
        /// Create a new filter expression 'And'ing 'this' with 'filter'.
        /// </summary>
        private FilterExpression And(FilterExpression filter)
        {
            return new FilterExpression(this, filter, true);
        }

        /// <summary>
        /// Create a new filter expression 'Or'ing 'this' with 'filter'.
        /// </summary>
        private FilterExpression Or(FilterExpression filter)
        {
            return new FilterExpression(this, filter, false);
        }

        /// <summary>
        /// Process the given operator from the filterStack.
        /// Puts back the result of operation back to filterStack.
        /// </summary>
        private static void ProcessOperator(Stack<FilterExpression> filterStack, Operator op)
        {
            if (op == Operator.And)
            {
                if (filterStack.Count < 2)
                {
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.MissingOperand));
                }

                var filterRight = filterStack.Pop();
                var filterLeft = filterStack.Pop();
                var result = filterLeft.And(filterRight);
                filterStack.Push(result);
            }
            else if (op == Operator.Or)
            {
                if (filterStack.Count < 2)
                {
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.MissingOperand));
                }

                var filterRight = filterStack.Pop();
                var filterLeft = filterStack.Pop();
                var result = filterLeft.Or(filterRight);
                filterStack.Push(result);
            }
            else if (op == Operator.OpenBrace)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.MissingCloseParenthesis));
            }
            else
            {
                Debug.Fail("ProcessOperator called for Unexpected operator.");
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, string.Empty));
            }
        }

        /// <summary>
        /// True, if filter is valid for given set of properties.
        /// When False, invalidProperties would contain properties making filter invalid.
        /// </summary>
        internal string[] ValidForProperties(IEnumerable<string> properties, Func<string, TestProperty> propertyProvider)
        {
            string[] invalidProperties = null;

            if (null == properties)
            {
                // if null, initialize to empty list so that invalid properties can be found.
                properties = Enumerable.Empty<string>();
            }

            bool valid = false;
            if (this.condition != null)
            {
                valid = this.condition.ValidForProperties(properties, propertyProvider);
                if (!valid)
                {
                    invalidProperties = new string[1] { this.condition.Name };
                }
            }
            else
            {
                invalidProperties = this.left.ValidForProperties(properties, propertyProvider);
                var invalidRight = this.right.ValidForProperties(properties, propertyProvider);
                if (null == invalidProperties)
                {
                    invalidProperties = invalidRight;
                }
                else if (null != invalidRight)
                {
                    invalidProperties = invalidProperties.Concat(invalidRight).ToArray();
                }
            }
            return invalidProperties;
        }

        /// <summary>
        /// Return FilterExpression after parsing the given filter expression, and a FastFilter when possible.
        /// </summary>
        internal static FilterExpression Parse(string filterString, out FastFilter fastFilter)
        {
            ValidateArg.NotNull(filterString, nameof(filterString));

            // Below parsing doesn't error out on pattern (), so explicitly search for that (empty parethesis).
            var invalidInput = Regex.Match(filterString, @"\(\s*\)");
            if (invalidInput.Success)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.EmptyParenthesis));
            }

            var tokens = TokenizeFilterExpressionString(filterString);
            var operatorStack = new Stack<Operator>();
            var filterStack = new Stack<FilterExpression>();

            var fastFilterBuilder = FastFilter.CreateBuilder();

            // This is based on standard parsing of in order expression using two stacks (operand stack and operator stack)
            // Precedence(And) > Precedence(Or)
            foreach (var inputToken in tokens)
            {
                var token = inputToken.Trim();
                if (string.IsNullOrEmpty(token))
                {
                    // ignore empty tokens
                    continue;
                }

                switch (token)
                {
                    case "&":
                    case "|":

                        Operator currentOperator = Operator.And;
                        if (string.Equals("|", token))
                        {
                            currentOperator = Operator.Or;
                        }

                        fastFilterBuilder.AddOperator(currentOperator);

                        // Always put only higher priority operator on stack.
                        //  if lesser priority -- pop up the stack and process the operator to maintain operator precedence.
                        //  if equal priority -- pop up the stack and process the operator to maintain operator associativity.
                        //  OpenBrace is special condition. & or | can come on top of OpenBrace for case like ((a=b)&c=d)
                        while (true)
                        {
                            bool isEmpty = operatorStack.Count == 0;
                            Operator stackTopOperator = isEmpty ? Operator.None : operatorStack.Peek();
                            if (isEmpty || stackTopOperator == Operator.OpenBrace || stackTopOperator < currentOperator)
                            {
                                operatorStack.Push(currentOperator);
                                break;
                            }
                            stackTopOperator = operatorStack.Pop();
                            ProcessOperator(filterStack, stackTopOperator);
                        }
                        break;

                    case "(":
                        operatorStack.Push(Operator.OpenBrace);
                        break;

                    case ")":
                        // process operators from the stack till OpenBrace is found.
                        // If stack is empty at any time, than matching OpenBrace is missing from the expression.
                        if (operatorStack.Count == 0)
                        {
                            throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.MissingOpenParenthesis));
                        }

                        Operator temp = operatorStack.Pop();
                        while (temp != Operator.OpenBrace)
                        {
                            ProcessOperator(filterStack, temp);
                            if (operatorStack.Count == 0)
                            {
                                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.MissingOpenParenthesis));
                            }
                            temp = operatorStack.Pop();
                        }

                        break;

                    default:
                        // push the operand to the operand stack.
                        Condition condition = Condition.Parse(token);
                        FilterExpression filter = new FilterExpression(condition);
                        filterStack.Push(filter);

                        fastFilterBuilder.AddCondition(condition);
                        break;
                }
            }
            while (operatorStack.Count != 0)
            {
                Operator temp = operatorStack.Pop();
                ProcessOperator(filterStack, temp);
            }

            if (filterStack.Count != 1)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CommonResources.TestCaseFilterFormatException, CommonResources.MissingOperator));
            }

            fastFilter = fastFilterBuilder.ToFastFilter();

            return filterStack.Pop();
        }

        /// <summary>
        /// Evaluate filterExpression with given propertyValueProvider.
        /// </summary>
        /// <param name="propertyValueProvider"> The property Value Provider.</param>
        /// <returns> True if evaluation is successful. </returns>
        internal bool Evaluate(Func<string, object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, nameof(propertyValueProvider));

            bool filterResult = false;
            if (null != this.condition)
            {
                filterResult = this.condition.Evaluate(propertyValueProvider);
            }
            else
            {
                // & or | operator
                bool leftResult = this.left.Evaluate(propertyValueProvider);
                bool rightResult = this.right.Evaluate(propertyValueProvider);
                if (this.areJoinedByAnd)
                {
                    filterResult = leftResult && rightResult;
                }
                else
                {
                    filterResult = leftResult || rightResult;
                }
            }
            return filterResult;
        }

        internal static IEnumerable<string> TokenizeFilterExpressionString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return TokenizeFilterExpressionStringHelper(str);

            IEnumerable<string> TokenizeFilterExpressionStringHelper(string s)
            {
                StringBuilder tokenBuilder = new StringBuilder();

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
                            // We just encountered "\\" (escaped '\'), this will set last to '\0'
                            // so the next char will not be treated as a suffix of escape sequence.
                            current = '\0';
                        }
                    }
                    else
                    {
                        switch (current)
                        {
                            case '(':
                            case ')':
                            case '&':
                            case '|':
                                if (tokenBuilder.Length > 0)
                                {
                                    yield return tokenBuilder.ToString();
                                    tokenBuilder.Clear();
                                }
                                yield return current.ToString();
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
}
