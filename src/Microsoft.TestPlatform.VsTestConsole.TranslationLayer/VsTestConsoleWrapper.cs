// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Vstest.console.exe Wrapper
    /// </summary>
    public class VsTestConsoleWrapper : IVsTestConsoleWrapper
    {
        #region Private Members

        private IProcessManager vstestConsoleProcessManager;

        private ITranslationLayerRequestSender requestSender;

        private bool sessionStarted;

        private const int ConnectionTimeout = 30 * 1000;

        private const string PORT_ARGUMENT = "/port:{0}";

        #endregion

        #region Constructor

        public VsTestConsoleWrapper(string vstestConsolePath) : 
            this(new VsTestConsoleRequestSender(), new VsTestConsoleProcessManager(vstestConsolePath))
        {
        }

        internal VsTestConsoleWrapper(ITranslationLayerRequestSender requestSender, IProcessManager processManager)
        {
            this.requestSender = requestSender;
            this.vstestConsoleProcessManager = processManager;
            this.sessionStarted = false;
        }

        #endregion

        #region IVsTestConsoleWrapper

        public void StartSession()
        {
            // Start communication
            var port = this.requestSender.InitializeCommunication();

            if (port > 0)
            {
                // Start Vstest.console.exe
                string args = string.Format(CultureInfo.InvariantCulture, PORT_ARGUMENT, port);
                this.vstestConsoleProcessManager.StartProcess(new string[1] { args });
            }
            else
            {
                // Close the sender as it failed to host server
                this.requestSender.Close();
                throw new TransationLayerException("Error hosting communication channel");
            }
        }

        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
        {
            EnsureInitialized();
            this.requestSender.InitializeExtensions(pathToAdditionalExtensions);
        }

        public void DiscoverTests(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            EnsureInitialized();
            this.requestSender.DiscoverTests(sources, discoverySettings, discoveryEventsHandler);
        }

        public void CancelDiscovery()
        {
            // TODO: Cancel Discovery
            //this.requestSender.CancelDiscovery();
        }

        public void RunTests(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            EnsureInitialized();
            this.requestSender.StartTestRun(sources, runSettings, testRunEventsHandler);
        }

        public void RunTests(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            EnsureInitialized();
            this.requestSender.StartTestRun(testCases, runSettings, testRunEventsHandler);
        }

        public void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(sources, runSettings, testRunEventsHandler, customTestHostLauncher);
        }

        public void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(testCases, runSettings, testRunEventsHandler, customTestHostLauncher);
        }

        public void CancelTestRun()
        { 
            this.requestSender.CancelTestRun();
        }
        
        public void AbortTestRun()
        {
            this.requestSender.AbortTestRun();
        }

        public void EndSession()
        {
            this.requestSender.EndSession();
            this.requestSender.Close();
            this.sessionStarted = false;
        }

        #endregion

        private void EnsureInitialized()
        {
            if (!this.sessionStarted && this.requestSender != null)
            {
                this.sessionStarted = this.requestSender.WaitForRequestHandlerConnection(ConnectionTimeout);
            }

            if(!this.sessionStarted)
            {
                throw new TransationLayerException("Error connecting to Vstest Command Line");
            }
        }
    }
}
