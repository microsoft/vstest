// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using System.Collections.Generic;
    using TestPlatform.CommandLine.Processors;
    using TestTools.UnitTesting;

    [TestClass]
    public class ArgumentProcessorFactoryTests
    {
        [TestMethod]
        public void BuildCommadMapsForProcessorWithValidShortCommandNameAddsShortCommandNameToMap()
        {
            var configProcessor = new ConfigurationArgumentProcessor();
            IEnumerable<IArgumentProcessor> processors = new List<IArgumentProcessor>()
            {
                //Adding Processor which has a Short Command Name
               configProcessor
            };
            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create(processors);

            Assert.IsTrue(factory.CommandToProcessorMap.ContainsKey("/c"));
            Assert.IsTrue(factory.CommandToProcessorMap.ContainsKey("/Configuration"));
            Assert.IsTrue(factory.CommandToProcessorMap.ContainsValue(configProcessor));
        }

        [TestMethod]
        public void BuildCommadMapsForProcessorWithIsSpecialCommandSetAddsProcessorToSpecialMap()
        {
            var sourceProcessor = new TestSourceArgumentProcessor();
            IEnumerable<IArgumentProcessor> processors = new List<IArgumentProcessor>(){sourceProcessor};
            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create(processors);

            Assert.IsTrue(factory.SpecialCommandToProcessorMap.ContainsKey("TestSource"));
            Assert.IsTrue(factory.SpecialCommandToProcessorMap.ContainsValue(sourceProcessor));
        }

        [TestMethod]
        public void BuildCommadMapsForMultipleProcessorAddsProcessorToAppropriateMaps()
        {
            var configProcessor = new ConfigurationArgumentProcessor();
            var sourceProcessor = new TestSourceArgumentProcessor();
            IEnumerable<IArgumentProcessor> processors = new List<IArgumentProcessor>() { sourceProcessor, configProcessor };
            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create(processors);

            Assert.IsTrue(factory.SpecialCommandToProcessorMap.ContainsKey("TestSource"));
            Assert.IsTrue(factory.SpecialCommandToProcessorMap.ContainsValue(sourceProcessor));
            Assert.IsTrue(factory.CommandToProcessorMap.ContainsKey("/c"));
            Assert.IsTrue(factory.CommandToProcessorMap.ContainsKey("/Configuration"));
            Assert.IsTrue(factory.CommandToProcessorMap.ContainsValue(configProcessor));
        }
    }
}

