// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    using TestPlatform.CommandLine.Processors;
    using TestTools.UnitTesting;

    [TestClass]
    public class ArgumentProcessorFactoryTests
    {
        [TestMethod]
        public void BuildCommadMapsForProcessorWithIsSpecialCommandSetAddsProcessorToSpecialMap()
        {
            var specialCommands = GetArgumentProcessors(specialCommandFilter: true)
                                    .Select(a => a.Metadata.Value.CommandName)
                                    .ToImmutableSortedSet();
            var factory = ArgumentProcessorFactory.Create();

            CollectionAssert.AreEquivalent(
                specialCommands,
                factory.SpecialCommandToProcessorMap.Keys.ToImmutableSortedSet());
        }

        [TestMethod]
        public void BuildCommadMapsForMultipleProcessorAddsProcessorToAppropriateMaps()
        {
            var commandProcessors = GetArgumentProcessors(specialCommandFilter: false);
            var commands = commandProcessors.Select(a => a.Metadata.Value.CommandName);
            var shortCommands = commandProcessors.Where(a => !string.IsNullOrEmpty(a.Metadata.Value.ShortCommandName))
                                    .Select(a => a.Metadata.Value.ShortCommandName);
            
            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            // Expect command processors to contain both long and short commands.
            CollectionAssert.AreEquivalent(
                commands.Concat(shortCommands).ToImmutableSortedSet(),
                factory.CommandToProcessorMap.Keys.ToImmutableSortedSet());
        }

        private static IEnumerable<IArgumentProcessor> GetArgumentProcessors(bool specialCommandFilter)
        {
            var allProcessors = typeof(ArgumentProcessorFactory).GetTypeInfo()
                                    .Assembly.GetTypes()
                                    .Where(t => !t.Name.Equals("IArgumentProcessor") && typeof(IArgumentProcessor).IsAssignableFrom(t));

            foreach (var processor in allProcessors)
            {
                var instance = Activator.CreateInstance(processor) as IArgumentProcessor;
                Assert.IsNotNull(instance, "Unable to instantiate processor: {0}", processor);

                var specialProcessor = instance.Metadata.Value.IsSpecialCommand;
                if ((specialCommandFilter && specialProcessor) || (!specialCommandFilter && !specialProcessor))
                {
                    yield return instance;
                }
            }
        }
    }
}