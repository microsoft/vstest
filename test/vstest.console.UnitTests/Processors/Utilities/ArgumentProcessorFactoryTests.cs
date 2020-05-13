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