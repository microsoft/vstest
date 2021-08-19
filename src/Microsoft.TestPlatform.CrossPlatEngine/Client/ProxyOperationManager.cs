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

    using CoreUtilities.Helpers;

    using CrossPlatEngineResources = Resources.Resources;
    using CommunicationUtilitiesResources = CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = CoreUtilities.Constants;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public class ProxyOperationManager
    {
        private readonly string versionCheckPropertyName = "IsVersionCheckRequired";
        private readonly string makeRunsettingsCompatiblePropertyName = "MakeRunsettingsCompatible";
        private readonly Guid id = Guid.NewGuid();
        private readonly ManualResetEventSlim testHostExited = new ManualResetEventSlim(false);
        private readonly IProcessHelper processHelper;

        private IBaseProxy baseProxy;
        private bool versionCheckRequired = true;
        private bool makeRunsettingsCompatible;
        private bool makeRunsettingsCompatibleSet;
        private bool initialized;
        private bool testHostLaunched;
        private int testHostProcessId;
        private string testHostProcessStdError;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class.
        /// </summary>
        ///
        /// <param name="requestData">Request data instance.</param>
        /// <param name="requestSender">Request sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        public ProxyOperationManager(
            IRequestData requestData,
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager)
            : this(
                  requestData,
                  requestSender,
                  testHostManager,
                  null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class.
        /// </summary>
        ///
        /// <param name="requestData">Request data instance.</param>
        /// <param name="requestSender">Request sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="baseProxy">The base proxy.</param>
        public ProxyOperationManager(
            IRequestData requestData,
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager,
            IBaseProxy baseProxy)
        {
            this.RequestData = requestData;
            this.RequestSender = requestSender;
            this.TestHostManager = testHostManager;
            this.baseProxy = baseProxy;

            this.initialized = false;
            this.testHostLaunched = false;
            this.testHostProcessId = -1;
            this.processHelper = new ProcessHelper();
            this.CancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the request data.
        /// </summary>
        public IRequestData RequestData { get; set; }

        /// <summary>
        /// Gets or sets the server for communication.
        /// </summary>
        public ITestRequestSender RequestSender { get; set; }

        /// <summary>
        /// Gets or sets the test host manager.
        /// </summary>
        public ITestRuntimeProvider TestHostManager { get; set; }

        /// <summary>
        /// Gets the proxy operation manager id.
        /// </summary>
        public Guid Id { get { return this.id; } }

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }
        #endregion

        #region IProxyOperationManager implementation.
        /// <summary>
        /// Initializes the proxy.
        /// </summary>
        ///
        /// <param name="skipDefaultAdapters">
        /// Flag indicating if we should skip the default adapters initialization.
        /// </param>
        public virtual void Initialize(bool skipDefaultAdapters)
        {
            // No-op.
        }

        /// <summary>
        /// Ensures that the engine is ready for test operations. Usually includes starting up the
        /// test host process.
        /// </summary>
        ///
        /// <param name="sources">List of test sources.</param>
        /// <param name="runSettings">Run settings to be used.</param>
        /// <param name="eventHandler">The events handler.</param>
        ///
        /// <returns>
        /// Returns true if the communication is established b/w runner and host, false otherwise.
        /// </returns>
        public virtual bool SetupChannel(
            IEnumerable<string> sources,
            string runSettings,
            ITestMessageEventHandler eventHandler)
        {
            return this.SetupChannel(sources, runSettings);
        }

        /// <summary>
        /// Ensures that the engine is ready for test operations. Usually includes starting up the
        /// test host process.
        /// </summary>
        ///
        /// <param name="sources">List of test sources.</param>
        /// <param name="runSettings">Run settings to be used.</param>
        ///
        /// <returns>
        /// Returns true if the communication is established b/w runner and host, false otherwise.
        /// </returns>
        public virtual bool SetupChannel(IEnumerable<string> sources, string runSettings)
        {
            this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();

            if (this.initialized)
            {
                return true;
            }

            var connTimeout = EnvironmentHelper.GetConnectionTimeout();

            this.testHostProcessStdError = string.Empty;
            TestHostConnectionInfo testHostConnectionInfo = this.TestHostManager.GetTestHostConnectionInfo();

            var portNumber = 0;
            if (testHostConnectionInfo.Role == ConnectionRole.Client)
            {
                portNumber = this.RequestSender.InitializeCommunication();
                testHostConnectionInfo.Endpoint += portNumber;
            }

            var processId = this.processHelper.GetCurrentProcessId();
            var connectionInfo = new TestRunnerConnectionInfo()
            {
                Port = portNumber,
                ConnectionInfo = testHostConnectionInfo,
                RunnerProcessId = processId,
                LogFile = this.GetTimestampedLogFile(EqtTrace.LogFile),
                TraceLevel = (int)EqtTrace.TraceLevel
            };

            // Subscribe to test host events.
            this.TestHostManager.HostLaunched += this.TestHostManagerHostLaunched;
            this.TestHostManager.HostExited += this.TestHostManagerHostExited;

            // Get environment variables from run settings.
            var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettings);

            // Get the test process start info.
            var testHostStartInfo = this.UpdateTestProcessStartInfo(
                this.TestHostManager.GetTestHostProcessStartInfo(
                    sources,
                    envVars,
                    connectionInfo));
            try
            {
                // Launch the test host.
                var hostLaunchedTask = this.TestHostManager.LaunchTestHostAsync(
                    testHostStartInfo,
                    this.CancellationTokenSource.Token);
                this.testHostLaunched = hostLaunchedTask.Result;

                if (this.testHostLaunched && testHostConnectionInfo.Role == ConnectionRole.Host)
                {
                    // If test runtime is service host, try to poll for connection as client.
                    this.RequestSender.InitializeCommunication();
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("ProxyOperationManager: Failed to launch testhost :{0}", ex);

                this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();
                throw new TestPlatformException(string.Format(
                    CultureInfo.CurrentUICulture,
                    CrossPlatEngineResources.FailedToLaunchTestHost,
                    ex.ToString()));
            }

            // Warn the user that execution will wait for debugger attach.
            var hostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
            var nativeHostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_NATIVE_DEBUG");

            if ((!string.IsNullOrEmpty(hostDebugEnabled) && hostDebugEnabled.Equals("1", StringComparison.Ordinal)) ||
                (new PlatformEnvironment().OperatingSystem.Equals(PlatformOperatingSystem.Windows) &&
                !string.IsNullOrEmpty(nativeHostDebugEnabled) && nativeHostDebugEnabled.Equals("1", StringComparison.Ordinal)))
            {
                ConsoleOutput.Instance.WriteLine(
                    CrossPlatEngineResources.HostDebuggerWarning,
                    OutputLevel.Warning);

                ConsoleOutput.Instance.WriteLine(
                    string.Format(
                        "Process Id: {0}, Name: {1}",
                        this.testHostProcessId,
                        this.processHelper.GetProcessName(this.testHostProcessId)),
                    OutputLevel.Information);

                // Increase connection timeout when debugging is enabled.
                connTimeout *= 5;
            }

            // If test host does not launch then throw exception, otherwise wait for connection.
            if (!this.testHostLaunched ||
                !this.RequestSender.WaitForRequestHandlerConnection(
                    connTimeout * 1000,
                    this.CancellationTokenSource.Token))
            {
                EqtTrace.Verbose($"Test host failed to start Test host launched:{testHostLaunched} test host exited: {testHostExited.IsSet}");
                // Throw a test platform exception with the appropriate message if user requested cancellation.
                this.CancellationTokenSource.Token.ThrowTestPlatformExceptionIfCancellationRequested();

                // Throw a test platform exception along with the error messages from the test if the test host exited unexpectedly
                // before communication was established.
                this.ThrowOnTestHostExited(this.testHostExited.IsSet);

                // Throw a test platform exception stating the connection to test could not be established even after waiting
                // for the configure timeout period.
                this.ThrowExceptionOnConnectionFailure(connTimeout);
            }

            // Handling special case for dotnet core projects with older test hosts.
            // Older test hosts are not aware of protocol version check, hence we should not be
            // sending VersionCheck message to these test hosts.
            this.CompatIssueWithVersionCheckAndRunsettings();

            if (this.versionCheckRequired)
            {
                this.RequestSender.CheckVersionWithTestHost();
            }

            this.initialized = true;

            return true;
        }

        /// <summary>
        /// Closes the channel and terminates the test host process.
        /// </summary>
        public virtual void Close()
        {
            bool? testHostExitedWithinTimeout = null;
            try
            {
                // Do not send message if the host did not launch.
                if (this.testHostLaunched)
                {
                    testHostExitedWithinTimeout = false;
                    this.RequestSender.EndSession();

                    // We want to give test host a chance to safely close.
                    // The upper bound for wait should be 100ms.
                    var timeout = int.TryParse(Environment.GetEnvironmentVariable("VSTEST_TESTHOST_TIMEOUT"), out var t) ? t : 100;
                    EqtTrace.Verbose("ProxyOperationManager.Close: waiting for test host to exit for {0} ms", timeout);
                    testHostExitedWithinTimeout = this.testHostExited.Wait(timeout);
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
                if (testHostExitedWithinTimeout == false)
                {
                    EqtTrace.Warning("ProxyOperationManager: Timed out waiting for test host to exit. Will terminate process.");
                }

                // Please clean up test host.
                this.TestHostManager.CleanTestHostAsync(CancellationToken.None).Wait();

                this.TestHostManager.HostExited -= this.TestHostManagerHostExited;
                this.TestHostManager.HostLaunched -= this.TestHostManagerHostLaunched;
            }
        }

        #endregion

        /// <summary>
        /// This method is exposed to enable derived classes to modify
        /// <see cref="TestProcessStartInfo"/>. For example, data collectors need additional
        /// environment variables to be passed.
        /// </summary>
        ///
        /// <param name="testProcessStartInfo">The test process start info.</param>
        ///
        /// <returns>
        /// The <see cref="TestProcessStartInfo"/>.
        /// </returns>
        public virtual TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            if (this.baseProxy == null)
            {
                // Update Telemetry Opt in status because by default in Test Host Telemetry is opted out
                var telemetryOptedIn = this.RequestData.IsTelemetryOptedIn ? "true" : "false";
                testProcessStartInfo.Arguments += " --telemetryoptedin " + telemetryOptedIn;
                return testProcessStartInfo;
            }

            return this.baseProxy.UpdateTestProcessStartInfo(testProcessStartInfo);
        }

        /// <summary>
        /// This function will remove the unknown run settings nodes from the run settings strings.
        /// This is necessary because older test hosts may throw exceptions when encountering
        /// unknown nodes.
        /// </summary>
        ///
        /// <param name="runsettingsXml">Run settings string.</param>
        ///
        /// <returns>The run settings after removing non-required nodes.</returns>
        public string RemoveNodesFromRunsettingsIfRequired(string runsettingsXml, Action<TestMessageLevel, string> logMessage)
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

        private string GetTimestampedLogFile(string logFile)
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

        private void CompatIssueWithVersionCheckAndRunsettings()
        {
            var properties = this.TestHostManager.GetType().GetRuntimeProperties();

            var versionCheckProperty = properties.FirstOrDefault(p => string.Equals(p.Name, versionCheckPropertyName, StringComparison.OrdinalIgnoreCase));
            if (versionCheckProperty != null)
            {
                this.versionCheckRequired = (bool)versionCheckProperty.GetValue(this.TestHostManager);
            }

            var makeRunsettingsCompatibleProperty = properties.FirstOrDefault(p => string.Equals(p.Name, makeRunsettingsCompatiblePropertyName, StringComparison.OrdinalIgnoreCase));
            if (makeRunsettingsCompatibleProperty != null)
            {
                this.makeRunsettingsCompatible = (bool)makeRunsettingsCompatibleProperty.GetValue(this.TestHostManager);
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

            // This needs to be set before we call the OnClientProcess exit because the
            // OnClientProcess will short-circuit WaitForRequestHandlerConnection in SetupChannel
            // that then continues to throw an exception and checks if the test host process exited.
            // If not it reports timeout, if we don't set this before OnClientProcessExit we will
            // report timeout even though we exited the test host before even attempting the connect.
            this.testHostExited.Set();
            this.RequestSender.OnClientProcessExit(this.testHostProcessStdError);
        }

        private void ThrowOnTestHostExited(bool testHostExited)
        {
            if (testHostExited)
            {
                // We might consider passing standard output here in case standard error is not
                // available because some errors don't end up in the standard error output.
                throw new TestPlatformException(string.Format(CrossPlatEngineResources.TestHostExitedWithError, this.testHostProcessStdError));
            }
        }

        private void ThrowExceptionOnConnectionFailure(int connTimeout)
        {
            // Failed to launch testhost process.
            var errorMsg = CrossPlatEngineResources.InitializationFailed;

            // Testhost launched but timeout occurred due to machine slowness.
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
                // Testhost failed with error.
                errorMsg = string.Format(CrossPlatEngineResources.TestHostExitedWithError, this.testHostProcessStdError);
            }

            throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, errorMsg));
        }
    }
}
