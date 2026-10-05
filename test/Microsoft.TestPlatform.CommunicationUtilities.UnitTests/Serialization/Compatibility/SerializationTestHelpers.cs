// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Shared helpers for serialization tests. Uses Newtonsoft JToken for JSON
/// normalization on all TFMs (Newtonsoft.Json is referenced on every TFM).
/// </summary>
internal static class SerializationTestHelpers
{
    /// <summary>
    /// The property ids a converter writes at a fixed position, in the order it writes them. Every
    /// other entry of a <c>Properties</c> array comes from the test object's property bag, which is
    /// a <c>ConcurrentDictionary</c> and enumerates in no defined order.
    /// </summary>
    private static readonly string[] FixedOrderProperties =
    [
        "TestCase.FullyQualifiedName",
        "TestCase.ExecutorUri",
        "TestCase.Source",
        "TestCase.CodeFilePath",
        "TestCase.DisplayName",
        "TestCase.Id",
        "TestCase.LineNumber",
        "TestResult.Outcome",
        "TestResult.ErrorMessage",
        "TestResult.ErrorStackTrace",
        "TestResult.DisplayName",
        "TestResult.ComputerName",
        "TestResult.Duration",
        "TestResult.StartTime",
        "TestResult.EndTime",
    ];

    /// <summary>
    /// Assert two JSON strings are semantically equal (ignoring whitespace).
    /// </summary>
    public static void AssertJsonEqual(string expected, string actual, string? message = null)
    {
        Assert.AreEqual(NormalizeForComparison(expected), NormalizeForComparison(actual),
            message ?? $"JSON mismatch.\nExpected:\n{expected}\n\nActual:\n{actual}");
    }

    /// <summary>
    /// Minify a JSON string to remove whitespace.
    /// </summary>
    public static string Minify(string json)
    {
        return Normalize(json);
    }

    private static string Normalize(string json)
    {
        return JToken.Parse(json).ToString(Formatting.None);
    }

    /// <summary>
    /// Minifies, and additionally puts every property bag in a comparable order.
    /// </summary>
    /// <remarks>
    /// Only the leading, converter-written properties of a <c>Properties</c> array have a defined
    /// order. The rest are enumerated from a <c>ConcurrentDictionary</c>, so their order is not
    /// stable between processes, and pinning a payload against it makes the pin flaky rather than
    /// strict. It looked positional only while every test object carried at most one such property.
    /// This is applied to comparison alone - <see cref="Minify"/> leaves a payload exactly as
    /// written, so a test feeding one back in as input still exercises the order it states.
    /// </remarks>
    private static string NormalizeForComparison(string json)
    {
        var token = JToken.Parse(json);
        OrderPropertyBags(token);

        return token.ToString(Formatting.None);
    }

    private static void OrderPropertyBags(JToken token)
    {
        switch (token)
        {
            case JObject obj:
                foreach (var property in obj.Properties().ToList())
                {
                    OrderPropertyBags(property.Value);

                    if (property.Name == "Properties" && property.Value is JArray bag)
                    {
                        property.Value = OrderPropertyBag(bag);
                    }
                }

                break;

            case JArray array:
                foreach (var item in array)
                {
                    OrderPropertyBags(item);
                }

                break;
        }
    }

    private static JArray OrderPropertyBag(JArray bag)
    {
        var entries = bag.Select(e => e.DeepClone()).ToList();
        var fixedOrder = entries.Where(e => Array.IndexOf(FixedOrderProperties, IdOf(e)) >= 0);
        var rest = entries
            .Where(e => Array.IndexOf(FixedOrderProperties, IdOf(e)) < 0)
            .OrderBy(IdOf, StringComparer.Ordinal);

        return new JArray(fixedOrder.Concat(rest));

        static string IdOf(JToken entry) => entry["Key"]?["Id"]?.ToString() ?? string.Empty;
    }
}
