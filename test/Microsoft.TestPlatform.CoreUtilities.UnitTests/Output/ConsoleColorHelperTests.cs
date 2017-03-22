// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Output
{
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class ConsoleColorHelperTests
    {
        [TestMethod]
        public void SetColorForActionShouldWorkForNullAction()
        {
            ConsoleColorHelper.SetColorForAction(ConsoleColor.White, null);
        }

        [TestMethod]
        public void SetColorForActionShouldSetGivenColorForAction()
        {
            var color = ConsoleColor.White;

            ConsoleColorHelper.SetColorForAction(ConsoleColor.Red, () => { color = Console.ForegroundColor; });

            Assert.IsTrue(color == ConsoleColor.Red);
        }

        [TestMethod]
        public void SetColorForActionShouldReSetColorAfterAction()
        {
            var color = Console.ForegroundColor;

            ConsoleColorHelper.SetColorForAction(color == ConsoleColor.Red? ConsoleColor.White : ConsoleColor.Red, () => { });

            Assert.IsTrue(color == Console.ForegroundColor);
        }

        [TestMethod]
        public void SetColorForActionShouldResetColorOnExceptionInAction()
        {
            var color = Console.ForegroundColor;

            Assert.ThrowsException<Exception>(() => ConsoleColorHelper.SetColorForAction(color == ConsoleColor.Red ? ConsoleColor.White : ConsoleColor.Red, () => { throw new Exception();}));

            Assert.IsTrue(color == Console.ForegroundColor);
        }
    }
}
