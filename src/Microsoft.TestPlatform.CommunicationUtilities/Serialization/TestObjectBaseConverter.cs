// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

/// <summary>
/// Converter factory for <see cref="TestObject"/>-derived types that don't have their own
/// dedicated converters (e.g. not TestCase or TestResult). Serializes only the property bag
/// as "Properties", matching the DataContract serialization behavior.
/// </summary>
internal class TestObjectBaseConverterFactory : JsonConverterFactory
{
    // Singleton converter handles all TestObject-derived types via the base class,
    // avoiding MakeGenericType + Activator.CreateInstance which fail under NativeAOT.
    private static readonly TestObjectBaseConverter Converter = new();

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(TestObject).IsAssignableFrom(typeToConvert)
            && typeToConvert != typeof(TestCase)
            && typeToConvert != typeof(TestResult);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return Converter;
    }
}

internal class TestObjectBaseConverter : JsonConverter<TestObject>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(TestObject).IsAssignableFrom(typeToConvert)
            && typeToConvert != typeof(TestCase)
            && typeToConvert != typeof(TestResult);
    }

    public override TestObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Create an instance of the requested type if possible. TestCase is the
        // fallback for abstract types or types without a parameterless constructor
        // because it is a concrete TestObject that preserves the property bag.
        var testObject = typeToConvert != typeof(TestObject) && !typeToConvert.IsAbstract
            ? (TestObject)(Activator.CreateInstance(typeToConvert) ?? new TestCase())
            : new TestCase();

        using var doc = JsonDocument.ParseValue(ref reader);
        var data = doc.RootElement;

        if (!data.TryGetProperty("Properties", out var properties) || properties.GetArrayLength() == 0)
        {
            return testObject;
        }

        foreach (var prop in properties.EnumerateArray())
        {
            if (!prop.TryGetProperty("Key", out var keyElement))
                continue;

            var testProperty = JsonSerializer.Deserialize<TestProperty>(keyElement.GetRawText(), options);
            if (testProperty is null)
                continue;

            if (!prop.TryGetProperty("Value", out var valueElement))
                continue;

            object? propertyData = null;
            if (valueElement.ValueKind != JsonValueKind.Null)
            {
                if (valueElement.ValueKind == JsonValueKind.String)
                {
                    propertyData = valueElement.GetString();
                }
                else
                {
                    propertyData = valueElement.GetRawText().Trim('"');
                }
            }

            testObject.SetPropertyValue(testProperty, propertyData, CultureInfo.InvariantCulture);
        }

        return testObject;
    }

    public override void Write(Utf8JsonWriter writer, TestObject value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Properties");
        writer.WriteStartArray();
        foreach (var property in value.GetProperties())
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            JsonSerializer.Serialize(writer, property.Key, options);
            writer.WritePropertyName("Value");
            if (property.Value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                WritePropertyValue(writer, property.Value, options);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a property value without using
    /// JsonSerializer.Serialize(writer, value, value.GetType()) which requires
    /// reflection metadata that NativeAOT trims.
    /// </summary>
    internal static void WritePropertyValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case string s: writer.WriteStringValue(s); break;
            case int i: writer.WriteNumberValue(i); break;
            case long l: writer.WriteNumberValue(l); break;
            case double d: writer.WriteNumberValue(d); break;
            case float f: writer.WriteNumberValue(f); break;
            case bool b: writer.WriteBooleanValue(b); break;
            case DateTimeOffset dto: writer.WriteStringValue(dto); break;
            case DateTime dt: writer.WriteStringValue(dt); break;
            case Guid g: writer.WriteStringValue(g); break;
            case Uri u: writer.WriteStringValue(u.OriginalString); break;
            case JsonElement je: je.WriteTo(writer); break;
            default:
                // For complex types (Traits, collections, etc.), serialize to JsonElement
                // first using the runtime type, then write the element. This avoids the
                // object? polymorphism problem while still producing valid JSON.
                var element = JsonSerializer.SerializeToElement(value, value.GetType(), options);
                element.WriteTo(writer);
                break;
        }
    }
}

#endif
