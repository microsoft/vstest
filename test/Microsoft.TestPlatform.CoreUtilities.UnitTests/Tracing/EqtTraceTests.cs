// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CoreUtilities.UnitTests
{
    using System.Diagnostics;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Text;

    [TestClass]
    public class EqtTraceTests
    {
        private static string dirPath = null;
        private static string logFile = null;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            // Set DoNotInitailize to false.
            EqtTrace.DoNotInitailize = false;
            dirPath = Path.Combine(Path.GetTempPath(), "TraceUT");
            try
            {
                Directory.CreateDirectory(dirPath);
                logFile = Path.Combine(dirPath, "trace.log");

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            EqtTrace.InitializeVerboseTrace(logFile);
        }

        [TestMethod]
        public void CheckInitializeLogFileTest()
        {
            Assert.AreEqual<string>(logFile, EqtTrace.LogFile, "Expected log file to be {0}", logFile);
        }

        [TestMethod]
        public void CheckIfTraceStateIsVerboseEnabled()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Verbose;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Verbose;
#endif
            Assert.AreEqual(true, EqtTrace.IsVerboseEnabled, "Expected trace state to be verbose actual state {0}", EqtTrace.IsVerboseEnabled);
        }

        [TestMethod]
        public void CheckIfTraceStateIsErrorEnabled()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Error;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Error;
#endif
            Assert.AreEqual(true, EqtTrace.IsErrorEnabled, "Expected trace state to be error actual state {0}", EqtTrace.IsErrorEnabled);
        }

        [TestMethod]
        public void CheckIfTraceStateIsInfoEnabled()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Info;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Info;
#endif
            Assert.AreEqual(true, EqtTrace.IsInfoEnabled, "Expected trace state to be info actual state {0}", EqtTrace.IsInfoEnabled);
        }

        [TestMethod]
        public void CheckIfTraceStateIsWarningEnabled()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Warning;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Warning;
#endif
            Assert.AreEqual(true, EqtTrace.IsWarningEnabled, "Expected trace state to be warning actual state {0}", EqtTrace.IsWarningEnabled);
        }

        [TestMethod]
        public void TraceShouldWriteError()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Error;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Error;
#endif
            EqtTrace.Error(new NotImplementedException());
            Assert.IsNotNull(ReadLogFile(), "Expected error message");
        }

        [TestMethod]
        public void TraceShouldWriteWarning()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Warning;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Warning;
#endif
            EqtTrace.Warning("Dummy Warning Message");
            Assert.IsTrue(ReadLogFile().Contains("Dummy Warning Message"), "Expected Warning message");
        }

        [TestMethod]
        public void TraceShouldWriteVerbose()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Verbose;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Verbose;
#endif
            EqtTrace.Verbose("Dummy Verbose Message");
            Assert.IsTrue(ReadLogFile().Contains("Dummy Verbose Message"), "Expected Verbose message");
        }
        [TestMethod]
        public void TraceShouldWriteInfo()
        {
#if NET451
            EqtTrace.TraceLevel = TraceLevel.Info;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Info;
#endif
            EqtTrace.Info("Dummy Info Message");
            Assert.IsTrue(ReadLogFile().Contains("Dummy Info Message"), "Expected Info message");
        }

        private string ReadLogFile()
        {
            string log = null;
            try
            {
                using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        log = sr.ReadToEnd();
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return log;
        }
    }
}
