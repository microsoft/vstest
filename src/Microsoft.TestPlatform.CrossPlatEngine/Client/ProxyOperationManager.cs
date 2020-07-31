// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public abstract class ProxyOperationManager
    {
        private readonly ITestRuntimeProvider testHostManager;
        private readonly IProcessHelper processHelper;
        private readonly string versionCheckPropertyName = "IsVersionCheckRequired";
        private readonly string makeRunsettingsCompatiblePropertyName = "MakeRunsettingsCompatible";
        private bool versionCheckRequired = true;
        private bool makeRunsettingsCompatible;
        private bool makeRunsettingsCompatibleSet;
        private readonly ManualResetEventSlim testHostExited = new ManualResetEventSlim(false);

        private int testHostProcessId;
        private bool initialized;
        private string testHostProcessStdError;
        private bool testHostLaunched;
        private IRequestData requestData;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class.
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        protected ProxyOperationManager(IRequestData requestData, ITestRequestSender requestSender, ITestRuntimeProvider testHostManager)
        {
            this.RequestSender = requestSender;
            this.CancellationTokenSource = new CancellationTokenSource();
            this.testHostManager = testHostManager;
            this.processHelper = new ProcessHelper();
            this.initialized = false;
            this.testHostLaunched = false;
            this.testHostProcessId = -1;
            this.requestData = requestData;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the server for communication.
        /// </summary>
        protected ITestRequestSender RequestSender { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; set; }

        #endregion

        #region IProxyOperationManager implementation.

        /// <summary>
        /// Ensure that the engine is ready for test operations.
        /// Usually includes starting up the test host process.
        /// </summary>
        /// <param name="sources">
        /// List of test sources.
        /// </param>
        /// <param name="cancellationToken">
        /// </param>
        /// <returns>
        /// Returns true if Communication is established b/w runner and host
        /// </returns>
        public virtual bool SetupChannel(IEnumerable<string> sources, string runSettings)
        {
            this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();
            var connTimeout = EnvironmentHelper.GetConnectionTimeout();

            if (!this.initialized)
            {
                this.testHostProcessStdError = string.Empty;
                TestHostConnectionInfo testHostConnectionInfo = this.testHostManager.GetTestHostConnectionInfo();
                
                var portNumber = 0;

                if (testHostConnectionInfo.Role == ConnectionRole.Client)
                {
                    portNumber = this.RequestSender.InitializeCommunication();
                    testHostConnectionInfo.Endpoint += portNumber;
                }

                var processId = this.processHelper.GetCurrentProcessId();
                var connectionInfo = new TestRunnerConnectionInfo { Port = portNumber, ConnectionInfo = testHostConnectionInfo, RunnerProcessId = processId, LogFile = this.GetTimestampedLogFile(EqtTrace.LogFile), TraceLevel = (int)EqtTrace.TraceLevel };

                // Subscribe to TestHost Event
                this.testHostManager.HostLaunched += this.TestHostManagerHostLaunched;
                this.testHostManager.HostExited += this.TestHostManagerHostExited;

                // Get envVars from run settings
                var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettings);

                // Get the test process start info
                var testHostStartInfo = this.UpdateTestProcessStartInfo(this.testHostManager.GetTestHostProcessStartInfo(sources, envVars, connectionInfo));
                try
                {
                    // Launch the test host.
                    var hostLaunchedTask = this.testHostManager.LaunchTestHostAsync(testHostStartInfo, this.CancellationTokenSource.Token);
                    this.testHostLaunched = hostLaunchedTask.Result;

                    if (this.testHostLaunched && testHostConnectionInfo.Role == ConnectionRole.Host)
                    {
                        // If test runtime is service host, try to poll for connection as client
                        this.RequestSender.InitializeCommunication();
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("ProxyOperationManager: Failed to launch testhost :{0}", ex);

                    this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CrossPlatEngineResources.FailedToLaunchTestHost, ex.ToString()));
                }

                // Warn the user that execution will wait for debugger attach.
                var hostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
                var nativeHostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_NATIVE_DEBUG");

                if (!string.IsNullOrEmpty(hostDebugEnabled) && hostDebugEnabled.Equals("1", StringComparison.Ordinal) ||
                    new PlatformEnvironment().OperatingSystem.Equals(PlatformOperatingSystem.Windows) &&
                    !string.IsNullOrEmpty(nativeHostDebugEnabled) && nativeHostDebugEnabled.Equals("1", StringComparison.Ordinal))
                {
                    ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.HostDebuggerWarning, OutputLevel.Warning);
                    ConsoleOutput.Instance.WriteLine(
                        string.Format("Process Id: {0}, Name: {1}", this.testHostProcessId, this.processHelper.GetProcessName(this.testHostProcessId)),
                        OutputLevel.Information);

                    // Increase connection timeout when debugging is enabled.
                    connTimeout *= 5;
                }

                // If TestHost does not launch then throw exception
                // If Testhost launches, wait for connection.
                if (!this.testHostLaunched ||
                    !this.RequestSender.WaitForRequestHandlerConnection(connTimeout * 1000, this.CancellationTokenSource.Token))
                {
                    EqtTrace.Verbose($"Test host failed to start Test host launched:{testHostLaunched} test host exited: {testHostExited.IsSet}");
                    // Throw a test platform exception with the appropriate message if user requested cancellation
                    this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();

                    // Throw a test platform exception along with the error messages from the test if the test host exited unexpectedly
                    // before communication was established
                    this.ThrowOnTestHostExited(this.testHostExited.IsSet);

                    // Throw a test platform exception stating the connection to test could not be established even after waiting
                    // for the configure timeout period
                    this.ThrowExceptionOnConnectionFailure(connTimeout);
                }

                // Handling special case for dotnet core projects with older test hosts
                // Older test hosts are not aware of protocol version check
                // Hence we should not be sending VersionCheck message to these test hosts
                this.CompatIssueWithVersionCheckAndRunsettings();

                if (this.versionCheckRequired)
                {
                    this.RequestSender.CheckVersionWithTestHost();
                }

                this.initialized = true;
            }

            return true;
        }

        /// <summary>
        /// Closes the channel, terminate test host process.
        /// </summary>
        public virtual void Close()
        {
            try
            {
                // do not send message if host did not launch
                if (this.testHostLaunched)
                {
                    this.RequestSender.EndSession();

                    // We want to give test host a chance to safely close.
                    // The upper bound for wait should be 100ms.
                    var timeout = 100;
                    EqtTrace.Verbose("ProxyOperationManager.Close: waiting for test host to exit for {0} ms", timeout);
                    this.testHostExited.Wait(timeout);
                }
            }
            catch (Exception ex)
            {
                // Error in sending an end session is not necessarily a failure. Discovery and execution should be already
                // complete at this time.
                EqtTrace.Warning("ProxyOperationManager: Failed to end session: " + ex);
            }
            finally
            {
                this.initialized = false;

                EqtTrace.Warning("ProxyOperationManager: Timed out waiting for test host to exit. Will terminate process.");

                // please clean up test host.
                this.testHostManager.CleanTestHostAsync(CancellationToken.None).Wait();

                this.testHostManager.HostExited -= this.TestHostManagerHostExited;
                this.testHostManager.HostLaunched -= this.TestHostManagerHostLaunched;
            }
        }

        #endregion

        /// <summary>
        /// This method is exposed to enable derived classes to modify TestProcessStartInfo. E.g. DataCollection need additional environment variables to be passed, etc.
        /// </summary>
        /// <param name="testProcessStartInfo">
        /// The sources.
        /// </param>
        /// <returns>
        /// The <see cref="TestProcessStartInfo"/>.
        /// </returns>
        protected virtual TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            // Update Telemetry Opt in status because by default in Test Host Telemetry is opted out
            var telemetryOptedIn = this.requestData.IsTelemetryOptedIn ? "true" : "false";
            testProcessStartInfo.Arguments += " --telemetryoptedin " + telemetryOptedIn;
            return testProcessStartInfo;
        }

        protected string GetTimestampedLogFile(string logFile)
        {
            if (string.IsNullOrWhiteSpace(logFile))
            {
                return null;
            }

            return Path.ChangeExtension(
                logFile,
                string.Format(
                    "host.{0}_{1}{2}",
                    DateTime.Now.ToString("yy-MM-dd_HH-mm-ss_fffff"),
                    new PlatformEnvironment().GetCurrentManagedThreadId(),
                    Path.GetExtension(logFile))).AddDoubleQuote();
        }

        /// <summary>
        /// This function will remove the unknown runsettings node from runsettings for old testhost who throws exception for unknown node.
        /// </summary>
        /// <param name="runsettingsXml">runsettings string</param>
        /// <returns>runsetting after removing un-required nodes</returns>
        protected string RemoveNodesFromRunsettingsIfRequired(string runsettingsXml, Action<TestMessageLevel, string> logMessage)
        {
            var updatedRunSettingsXml = runsettingsXml;
            if (!this.makeRunsettingsCompatibleSet)
            {
                this.CompatIssueWithVersionCheckAndRunsettings();
            }

            if (this.makeRunsettingsCompatible)
            {
                logMessage.Invoke(TestMessageLevel.Warning, CrossPlatEngineResources.OldTestHostIsGettingUsed);
                updatedRunSettingsXml = InferRunSettingsHelper.MakeRunsettingsCompatible(runsettingsXml);
            }

            return updatedRunSettingsXml;
        }

        private void CompatIssueWithVersionCheckAndRunsettings()
        {
            var properties = this.testHostManager.GetType().GetRuntimeProperties();

            var versionCheckProperty = properties.FirstOrDefault(p => string.Equals(p.Name, versionCheckPropertyName, StringComparison.OrdinalIgnoreCase));
            if (versionCheckProperty != null)
            {
                this.versionCheckRequired = (bool)versionCheckProperty.GetValue(this.testHostManager);
            }

            var makeRunsettingsCompatibleProperty = properties.FirstOrDefault(p => string.Equals(p.Name, makeRunsettingsCompatiblePropertyName, StringComparison.OrdinalIgnoreCase));
            if (makeRunsettingsCompatibleProperty != null)
            {
                this.makeRunsettingsCompatible = (bool)makeRunsettingsCompatibleProperty.GetValue(this.testHostManager);
                this.makeRunsettingsCompatibleSet = true;
            }
        }

        private void TestHostManagerHostLaunched(object sender, HostProviderEventArgs e)
        {
            EqtTrace.Verbose(e.Data);
            this.testHostProcessId = e.ProcessId;
        }

        private void TestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            EqtTrace.Verbose("CrossPlatEngine.TestHostManagerHostExited: calling on client process exit callback.");
            this.testHostProcessStdError = e.Data;

            // this needs to be set before we call the OnClientProcess exit
            // because the OnClientProcess will short-circuit WaitForRequestHandlerConnection in SetupChannel
            // that then continues to throw an exception and checks if the testhost process exited
            // if not it reports timeout, if we don't set this before OnClientProcessExit we will report timeout
            // even though we exited the test host before even attempting the connect
            this.testHostExited.Set();
            this.RequestSender.OnClientProcessExit(this.testHostProcessStdError);
        }

        private void ThrowOnTestHostExited(bool testHostExited)
        {            
            if (testHostExited)
            {
                // we might consider passing standard output here in case standard error is not available because some 
                // errors don't end up in the standard error output
                throw new TestPlatformException(string.Format(CrossPlatEngineResources.TestHostExitedWithError, this.testHostProcessStdError));
            }
        }

        private void ThrowExceptionOnConnectionFailure(int connTimeout)
        {
            // Failed to launch testhost process.
            var errorMsg = CrossPlatEngineResources.InitializationFailed;

            // Testhost launched but Timeout occurred due to machine slowness.
            if (this.testHostLaunched)
            {
                errorMsg = string.Format(
                    CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                    CoreUtilitiesConstants.VstestConsoleProcessName,
                    CoreUtilitiesConstants.TesthostProcessName,
                    connTimeout,
                    EnvironmentHelper.VstestConnectionTimeout);
            }

            // After testhost process launched failed with error.
            if (!string.IsNullOrWhiteSpace(this.testHostProcessStdError))
            {
                // Testhost failed with error
                errorMsg = string.Format(CrossPlatEngineResources.TestHostExitedWithError, this.testHostProcessStdError);
            }

            throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, errorMsg));
        }
    }
}
