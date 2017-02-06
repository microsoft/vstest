//// Copyright (c) Microsoft Corporation. All rights reserved.
//// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading;

    public class Program
    {
        private const string PORT_ARGUMENT = "/port:{0}";
        private const string PARENT_PROCESSID_ARGUMENT = "/parentprocessid:{0}";

        private static SocketCommunicationManager communicationManager;
        private static JsonDataSerializer dataSerializer = JsonDataSerializer.Instance;

        public static int Main(string[] args)
        {
            if(args == null || args.Length < 1)
            {
                Console.WriteLine("Please provide appropriate arguments. Arguments can be passed as following:");
                Console.WriteLine("Microsoft.TestPlatform.Protocol.exe --testassembly:\"[assemblyPath]\" --operation:\"[RunAll|RunSelected|Discovery|DebugAll]\" --testadapterpath:\"[path]\"");
                Console.WriteLine("or Microsoft.TestPlatform.Protocol.exe -a:\"[assemblyPath]\" -o:\"[RunAll|RunSelected|Discovery|DebugAll]\" -p:\"[path]\" \n");

                return 1;
            }

            var executingLocation = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            // Default values
            var testAssembly = Path.Combine(executingLocation, "UnitTestProject.dll");
            string testadapterPath = null;
            string operation = "Discovery";
            var separator = new char[] { ':' };
            foreach (var arg in args)
            {
                if (arg.StartsWith("-p:") || arg.StartsWith("--testadapterpath:"))
                {
                    testadapterPath = arg.Split(separator, 2)[1];
                }
                else if (arg.StartsWith("-a:") || arg.StartsWith("--testassembly:"))
                {
                    testAssembly = arg.Split(separator, 2)[1];
                }
                else if (arg.StartsWith("-o:") || arg.StartsWith("--operation:"))
                {
                    operation = arg.Split(separator, 2)[1];
                }
            }

            Console.WriteLine("TestAdapter Path : {0}", testadapterPath);
            Console.WriteLine("TestAssembly Path : {0}", testAssembly);
            Console.WriteLine("Operation : {0}", operation);

            var processManager = new RunnerProcessManager();
            communicationManager = new SocketCommunicationManager();

            // Start the server
            var port = communicationManager.HostServer();

            // Start runner exe and wait for the connection
            string parentProcessIdArgs = string.Format(CultureInfo.InvariantCulture, PARENT_PROCESSID_ARGUMENT, Process.GetCurrentProcess().Id);
            string portArgs = string.Format(CultureInfo.InvariantCulture, PORT_ARGUMENT, port);
            processManager.StartProcess(new string[2] { parentProcessIdArgs, portArgs });

            communicationManager.AcceptClientAsync().Wait();
            communicationManager.WaitForClientConnection(Timeout.Infinite);
            HandShakeWithVsTestConsole();

            // Actual operation
            dynamic discoveredTestCases;
            switch (operation.ToLower())
            {
                case "discovery":
                    discoveredTestCases = DiscoverTests(testadapterPath, testAssembly);
                    break;
               
                case "runselected":
                    discoveredTestCases = DiscoverTests(testadapterPath, testAssembly);
                    RunSelectedTests(discoveredTestCases);
                    break;

                case "debugall":
                    DebugAllTests(new List<string>() { testAssembly });
                    break;

                case "runall":
                default:
                    RunAllTests(new List<string>() { testAssembly });
                    break;
            }

            return 0;
        }

        static void HandShakeWithVsTestConsole()
        {
            // HandShake with vstest.console
            Console.WriteLine("=========== HandShake with vstest.console ==========");
            var message = communicationManager.ReceiveMessage();
            if (message.MessageType == MessageType.SessionConnected)
            {
                // Version Check
                communicationManager.SendMessage(MessageType.VersionCheck);
                message = communicationManager.ReceiveMessage();

                if (message.MessageType == MessageType.VersionCheck)
                {
                    var version = JsonDataSerializer.Instance.DeserializePayload<int>(message);
                    
                    var success = version == 1;
                    Console.WriteLine("Version Success: {0}", success);
                }
            }
        }

        static dynamic DiscoverTests(string testadapterPath, string testAssembly)
        {
            Console.WriteLine("Starting Operation : Discovery");

            // Intialize the extensions
            if (testadapterPath != null)
            {
                communicationManager.SendMessage(MessageType.ExtensionsInitialize, new List<string>() { testadapterPath });
            }

            // Start Discovery
            communicationManager.SendMessage(
                           MessageType.StartDiscovery,
                           new DiscoveryRequestPayload() { Sources = new List<string>() { testAssembly }, RunSettings = null });
            var isDiscoveryComplete = false;

            dynamic testCases = null;

            while (!isDiscoveryComplete)
            {
                var message = communicationManager.ReceiveMessage();

                if (string.Equals(MessageType.TestCasesFound, message.MessageType))
                {
                    // Handle discovered tests here.
                    testCases = (JsonDataSerializer.Instance.DeserializePayload<dynamic>(message));
                }
                else if (string.Equals(MessageType.DiscoveryComplete, message.MessageType))
                {
                    dynamic discoveryCompletePayload =
                        JsonDataSerializer.Instance.DeserializePayload<dynamic>(message);
                    
                    // Handle discovery complete here
                    isDiscoveryComplete = true;
                }
                else if (string.Equals(MessageType.TestMessage, message.MessageType))
                {
                    var testMessagePayload = dataSerializer.DeserializePayload<dynamic>(message);
                    // TODO: Handle messages here.
                }
            }

            return testCases;
        }
        
        static void RunAllTests(List<string> sources)
        {
            Console.WriteLine("Starting Operation: RunAll");
            communicationManager.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, new TestRunRequestPayload() { Sources = sources, RunSettings = null });
            RecieveRunMesagesAndHandleRunComplete();
        }
        
        static void RunSelectedTests(dynamic testCases)
        {
            Console.WriteLine("Starting Operation: RunSelected");
            communicationManager.SendMessage(MessageType.TestRunSelectedTestCasesDefaultHost, new TestRunRequestPayload() { TestCases = testCases, RunSettings = null });
            RecieveRunMesagesAndHandleRunComplete();
        }

        static void DebugAllTests(List<string> sources)
        {
            Console.WriteLine("Starting Operation: DebugAll");
            communicationManager.SendMessage(MessageType.GetTestRunnerProcessStartInfoForRunAll, new TestRunRequestPayload()
            {
                Sources = sources,
                RunSettings = null,
                DebuggingEnabled = true
            });
            RecieveRunMesagesAndHandleRunComplete();
        }

        static void RecieveRunMesagesAndHandleRunComplete()
        {
            var isTestRunComplete = false;

            while (!isTestRunComplete)
            {
                var message = communicationManager.ReceiveMessage();

                if (string.Equals(MessageType.TestRunStatsChange, message.MessageType))
                {
                    var testRunChangedArgs = dataSerializer.DeserializePayload<dynamic>(message);
                    // Handle TestRunStatsChange here
                }
                else if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
                {
                    var testRunCompletePayload = dataSerializer.DeserializePayload<dynamic>(message);

                    // Handle TestRunComplete here
                    // Set the flag, to end the loop.
                    isTestRunComplete = true;
                }
                else if (string.Equals(MessageType.TestMessage, message.MessageType))
                {
                    var testMessagePayload = dataSerializer.DeserializePayload<dynamic>(message);
                    // TODO: Handle log messages here
                }
                else if (string.Equals(MessageType.CustomTestHostLaunch, message.MessageType))
                {
                    var testProcessStartInfo = dataSerializer.DeserializePayload<dynamic>(message);

                    // Launch Test Host here and Send the acknowledgement
                    var ackPayload = new CustomHostLaunchAckPayload() { HostProcessId = -1, ErrorMessage = null };

                    Process process = new Process();
                    process.StartInfo.FileName = testProcessStartInfo.FileName;
                    process.StartInfo.Arguments = testProcessStartInfo.Arguments;
                    process.StartInfo.WorkingDirectory = testProcessStartInfo.WorkingDirectory;
                    process.Start();

                    ackPayload.HostProcessId = process.Id;
                    communicationManager.SendMessage(MessageType.CustomTestHostLaunchCallback, ackPayload);
                }
            }
        }
    }

    public class CustomHostLaunchAckPayload
    {
        /// <summary>
        /// ProcessId of the TestHost launched by Clients like IDE, LUT etc.
        /// </summary>
        [DataMember]
        public int HostProcessId { get; set; }

        /// <summary>
        /// ErrorMessage, in cases where custom launch fails
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }
    }
}