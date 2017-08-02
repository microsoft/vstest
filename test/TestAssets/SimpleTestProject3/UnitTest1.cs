// Copyright (c) Microsoft. All rights reserved.

namespace SampleUnitTestProject3
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading;
#if NET451
    using System.Windows.Forms;
#endif

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void WorkingDirectoryTest()
        {
            Assert.AreEqual(Path.GetDirectoryName(typeof(UnitTest1).GetTypeInfo().Assembly.Location), Directory.GetCurrentDirectory());
        }

        [TestMethod]
        public void ExitwithUnhandleException()
        {
            Action fail = () => throw new InvalidOperationException();
            var thread = new Thread(new ThreadStart(fail));
            thread.Start();
            thread.Join();
        }

        [TestMethod]
        public void ExitWithStackoverFlow()
        {
            ExitWithStackoverFlow();
        }

#if NET451
        [TestMethod]
        public void UITestMethod()
        {
            Clipboard.SetText("Clipboard");
        }

        [TestMethod]
        public void UITestWithSleep1()
        {
            Clipboard.SetText("Clipboard");
            Thread.Sleep(1000 * 5);
        }

        [TestMethod]
        public void UITestWithSleep2()
        {
            Clipboard.SetText("Clipboard");
            Thread.Sleep(1000 * 10);
        }
#endif
    }
}
