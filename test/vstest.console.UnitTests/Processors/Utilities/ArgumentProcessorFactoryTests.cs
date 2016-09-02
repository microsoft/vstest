// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using System.Collections.Generic;
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
                                    .ToList();

            List<string> xplatspecialCommands = new List<string>();

            // for each command add there xplat version
            foreach (string name in specialCommands)
            {
                xplatspecialCommands.Add(string.Concat("--", name.Remove(0, 1)));
            }
            var factory = ArgumentProcessorFactory.Create();

            CollectionAssert.AreEquivalent(
                specialCommands.Concat(xplatspecialCommands).ToList(),
                factory.SpecialCommandToProcessorMap.Keys.ToList());
        }

        [TestMethod]
        public void BuildCommadMapsForMultipleProcessorAddsProcessorToAppropriateMaps()
        {
            var commandProcessors = GetArgumentProcessors(specialCommandFilter: false);
            var commands = commandProcessors.Select(a => a.Metadata.Value.CommandName);
            List<string> xplatCommandName = new List<string>();

            // for each command add there xplat version
            foreach (string name in commands)
            {
                xplatCommandName.Add(string.Concat("--", name.Remove(0, 1)));
            }

            var shortCommands = commandProcessors.Where(a => !string.IsNullOrEmpty(a.Metadata.Value.ShortCommandName))
                                    .Select(a => a.Metadata.Value.ShortCommandName);

            List<string> xplatShortCommandName = new List<string>();

            // for each short command add there xplat version
            foreach (string name in shortCommands)
            {
                xplatShortCommandName.Add(name.Replace('/', '-'));
            }

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            // Expect command processors to contain both long and short commands.
            CollectionAssert.AreEquivalent(
                commands.Concat(xplatCommandName).Concat(shortCommands).Concat(xplatShortCommandName).ToList(),
                factory.CommandToProcessorMap.Keys.ToList());
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