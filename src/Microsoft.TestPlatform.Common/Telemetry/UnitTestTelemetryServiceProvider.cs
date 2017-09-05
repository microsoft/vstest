// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System;
    using System.IO;
    using System.Text;

    /// <inheritdoc />
    /// <summary>
    /// Dummy Implementation of Unit test Telemetry Service Provider.
    /// </summary>
    public class UnitTestTelemetryServiceProvider : IUnitTestTelemetryServiceProvider
    {
        private const string DirectoryPath = @"c:\temp";
        private const string FilePath = @"c:\temp\MyTest.txt";

        public void Dispose()
        {
            // throw new NotImplementedException();
        }

        public void LogEvent(string eventName, string property, string value)
        {
            String[] arr = {
                eventName,
                property,
                value
            };

            if (!File.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }

            // This text is added only once to the file.
            if (!File.Exists(FilePath))
            {
                File.WriteAllLines(FilePath, arr, Encoding.UTF8);
            }

            File.AppendAllLines(FilePath, arr, Encoding.UTF8);
        }

        public void PostEvent(string eventName)
        {
          //  throw new NotImplementedException();
        }
    }
}
