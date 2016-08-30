// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Defines the test run stats header
    /// </summary>
    [DataContract]
    public class TestRunStatistics : ITestRunStatistics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunStatistics"/> class.
        /// </summary>
        /// <param name="stats"> The stats. </param>
        public TestRunStatistics(IDictionary<TestOutcome, long> stats)
        {
            this.Stats = stats;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunStatistics"/> class.
        /// </summary>
        /// <param name="executedTests"> The executed tests. </param>
        /// <param name="stats"> The stats. </param>
        /// <remarks> This constructor is only needed to reconstruct the object during deserialization.</remarks>
        [JsonConstructor]
        public TestRunStatistics(long executedTests, IDictionary<TestOutcome, long> stats)
        {
            this.ExecutedTests = executedTests;
            this.Stats = stats;
        }

        /// <summary>
        /// Gets or sets the number of tests that have been run.
        /// </summary>
        [DataMember]
        public long ExecutedTests { get; set; }

        /// <summary>
        /// Gets the test stats which is the test outcome versus its state.
        /// </summary>
        [DataMember]
        public IDictionary<TestOutcome, long> Stats { get; private set; }

        /// <summary>
        /// Gets the number of tests with a specified outcome.
        /// </summary>
        /// <param name="testOutcome"> The test outcome. </param>
        /// <returns> The number of tests with this outcome. </returns>
        public long this[TestOutcome testOutcome]
        {
            get
            {
                long count;
                if (this.Stats.TryGetValue(testOutcome, out count))
                {
                    return count;
                }

                return 0;
            }
        }
    }

    /// <summary>
    /// Json converter for converting ITestRunStatistics to TestRunStatistics
    /// </summary>
    public class TestRunStatisticsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.Equals(typeof(TestRunStatistics));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<TestRunStatistics>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //use the default serialization - it works fine
            serializer.Serialize(writer, value);
        }
    }
}
