// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleWindowCheck;

[TestClass]
public class UnitTest1
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    /// <summary>
    /// Reports whether the current process has a console window.
    /// The integration test checks the pass/fail result and error message
    /// to verify CreateNoNewWindow behavior.
    /// </summary>
    [TestMethod]
    public void ReportConsoleWindowStatus()
    {
        var hasConsoleWindow = GetConsoleWindow() != IntPtr.Zero;
        Assert.IsTrue(hasConsoleWindow, $"HAS_CONSOLE_WINDOW={hasConsoleWindow}");
    }
}
