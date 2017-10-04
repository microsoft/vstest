// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void CreateArgumentProcessorIsTreatingNonArgumentAsSource()
        {
            string argument = "--NonArgumet:Dummy";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            IArgumentProcessor result = factory.CreateArgumentProcessor(argument);

            Assert.AreEqual(typeof(TestSourceArgumentProcessor), result.GetType());
        }

        [TestMethod]
        public void CreateArgumentProcessorIsTreatingNonArgumentAsSourceEvenItIsStratingFromForwardSlash()
        {
            string argument = "/foo/foo.dll";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            IArgumentProcessor result = factory.CreateArgumentProcessor(argument);

            Assert.AreEqual(typeof(TestSourceArgumentProcessor), result.GetType());
        }

        [TestMethod]
        public void CreateArgumentProcessorShouldReturnPlatformArgumentProcessorWhenArgumentIsPlatform()
        {
            string argument = "/Platform:x64";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            IArgumentProcessor result = factory.CreateArgumentProcessor(argument);

            Assert.AreEqual(typeof(PlatformArgumentProcessor), result.GetType());
        }

        [TestMethod]
        public void CreateArgumentProcessorShouldReturnPlatformArgumentProcessorWhenArgumentIsPlatformInXplat()
        {
            string argument = "--Platform:x64";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            IArgumentProcessor result = factory.CreateArgumentProcessor(argument);

            Assert.AreEqual(typeof(PlatformArgumentProcessor), result.GetType());
        }

        [TestMethod]
        public void CreateArgumentProcessorShouldReturnThrowExceptionIfArgumentsIsNull()
        {
            var command = "--";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();
            Action action = () => factory.CreateArgumentProcessor(command, null);

            ExceptionUtilities.ThrowsException<ArgumentException>(
           action,
                "Cannot be null or empty", "argument");
        }

        [TestMethod]
        public void CreateArgumentProcessorShouldReturnNullIfInvalidCommandIsPassed()
        {
            var command = "/-";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            IArgumentProcessor result = factory.CreateArgumentProcessor(command, new string[] { "" });

            Assert.IsNull(result);
        }

        [TestMethod]
        public void CreateArgumentProcessorShouldReturnCLIRunSettingsArgumentProcessorIfCommandIsGiven()
        {
            var command = "--";

            ArgumentProcessorFactory factory = ArgumentProcessorFactory.Create();

            IArgumentProcessor result = factory.CreateArgumentProcessor(command, new string[] { "" });

            Assert.AreEqual(typeof(CLIRunSettingsArgumentProcessor), result.GetType());
        }

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
                                    .Where(t => !t.GetTypeInfo().IsAbstract && !t.Name.Equals("IArgumentProcessor") && typeof(IArgumentProcessor).IsAssignableFrom(t));

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