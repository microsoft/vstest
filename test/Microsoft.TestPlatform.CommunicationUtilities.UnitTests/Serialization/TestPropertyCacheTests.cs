// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.TestDoubles;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

[TestClass]
[DoNotParallelize]
public class TestPropertyCacheTests
{
    [TestMethod]
    public async Task ConcurrentDeserializationShouldPreserveColdCacheTypesAndValues()
    {
        const int rounds = 32;
        object[] values = { 29, 42L, (short)7, true, Guid.NewGuid(), new Uri("https://example.com/"), 3.5, "value" };
        var cache = GetTypeCache();
        var typeNames = new string[rounds, values.Length];
        var payloads = new string[rounds, values.Length];
        var whitespace = 1;
        for (var round = 0; round < rounds; round++)
        {
            for (var worker = 0; worker < values.Length; worker++)
            {
                var type = values[worker].GetType();
                string typeName;
                do
                {
                    // Assembly-name whitespace gives fresh cache keys without changing the resolved type.
                    typeName = type.FullName + "," + new string(' ', whitespace++) + type.Assembly.FullName;
                }
                while (cache.ContainsKey(typeName));

                Assert.AreEqual(type, Type.GetType(typeName));
                typeNames[round, worker] = typeName;
                payloads[round, worker] = CreatePayload(typeName, values[worker]);
            }
        }

        using var barrier = new Barrier(values.Length);
        var workers = Enumerable.Range(0, values.Length).Select(worker => Task.Factory.StartNew(
            () =>
            {
                for (var round = 0; round < rounds; round++)
                {
                    Assert.IsTrue(barrier.SignalAndWait(10000));
                    AssertDeserializedValue(payloads[round, worker], values[worker].GetType(), values[worker]);
                    Assert.IsTrue(barrier.SignalAndWait(10000));
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default)).ToArray();

        await Task.WhenAll(workers);

        for (var round = 0; round < rounds; round++)
        {
            for (var worker = 0; worker < values.Length; worker++)
            {
                Assert.AreEqual(values[worker].GetType(), cache[typeNames[round, worker]]);
                AssertDeserializedValue(payloads[round, worker], values[worker].GetType(), values[worker]);
            }
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TypeCacheShouldKeepCaseDistinctResolutions(bool resolveLowercaseFirst)
    {
        var assemblyName = "Missing.CacheCase." + Guid.NewGuid().ToString("N");
        var uppercase = "System.Int32, " + assemblyName;
        var lowercase = "system.int32, " + assemblyName;
        var integerPayload = CreatePayload(uppercase, 29);
        var stringPayload = CreatePayload(lowercase, "29");
        var cache = GetTypeCache();
        Assert.IsFalse(cache.ContainsKey(uppercase));
        Assert.IsFalse(cache.ContainsKey(lowercase));

        if (resolveLowercaseFirst)
        {
            AssertDeserializedValue(stringPayload, typeof(string), "29");
            AssertDeserializedValue(integerPayload, typeof(int), 29);
        }
        else
        {
            AssertDeserializedValue(integerPayload, typeof(int), 29);
            AssertDeserializedValue(stringPayload, typeof(string), "29");
        }

        Assert.AreEqual(typeof(int), cache[uppercase]);
        Assert.AreEqual(typeof(string), cache[lowercase]);
        AssertDeserializedValue(integerPayload, typeof(int), 29);
        AssertDeserializedValue(stringPayload, typeof(string), "29");
    }

    private static IDictionary<string, Type> GetTypeCache()
    {
        var field = typeof(TestProperty).GetField("TypeCache", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (IDictionary<string, Type>)field.GetValue(null)!;
    }

    private static string CreatePayload(string typeName, object value)
        => JsonDataSerializer.Instance.Serialize(new
        {
            Properties = new[]
            {
                new
                {
                    Key = new
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Label = "cache regression",
                        Category = string.Empty,
                        Description = string.Empty,
                        Attributes = 0,
                        ValueType = typeName
                    },
                    Value = value
                }
            }
        });

    private static void AssertDeserializedValue(string payload, Type expectedType, object expectedValue)
    {
        var result = JsonDataSerializer.Instance.Deserialize<TestableTestObject>(payload)!;
        var property = result.Properties.Single();
        Assert.AreEqual(expectedType, property.GetValueType());
        var value = result.GetPropertyValue(property);
        Assert.IsNotNull(value);
        Assert.AreEqual(expectedType, value.GetType());
        Assert.AreEqual(expectedValue, value);
    }
}
