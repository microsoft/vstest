// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class MessageRedactionTests
    {
        [TestMethod]
        [DataRow("SomeFuturePassword")]
        [DataRow("A_TOKEN_WE_HAVE_NOT_INVENTED_YET")]
        public void MessageToStringShouldRedactSecretNamedValues(string variableName)
        {
            const string secret = "DO-NOT-LOG-THIS";
            var message = new Message
            {
                MessageType = MessageType.TestMessage,
                Payload = JToken.Parse($"{{\"EnvironmentVariables\":{{\"{variableName}\":\"{secret}\"}}}}"),
            };

            Assert.IsFalse(message.ToString().Contains(secret));
            StringAssert.Contains(message.ToString(), "<redacted>");
            StringAssert.Contains(message.ToString(), variableName);
        }

        [TestMethod]
        public void MessageToStringShouldKeepNonSecretValues()
        {
            var message = new Message
            {
                MessageType = MessageType.TestMessage,
                Payload = JToken.Parse("{\"EnvironmentVariables\":{\"DOTNET_ROOT\":\"C:\\\\dotnet\",\"MY_SECRET\":\"hide-me\"}}"),
            };

            var logged = message.ToString();

            StringAssert.Contains(logged, "C:\\\\dotnet");
            Assert.IsFalse(logged.Contains("hide-me"));
        }

        [TestMethod]
        public void MessageToStringShouldKeepOrdinaryPayload()
        {
            var message = new Message
            {
                MessageType = MessageType.TestMessage,
                Payload = JToken.Parse("{\"Source\":\"test.dll\"}"),
            };

            StringAssert.Contains(message.ToString(), "test.dll");
            Assert.IsFalse(message.ToString().Contains("<redacted>"));
        }
    }
}
