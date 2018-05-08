// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System.Globalization;
    using System.Text;
    using Interfaces;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// The VangurdCommandBuilder class.
    /// </summary>
    internal class VangurdCommandBuilder : IVangurdCommandBuilder
    {
        /// <inheritdoc />
        public string GenerateCommandLine(
            VangurdCommand vangurdCommand,
            string sessionName,
            string outputName,
            string configurationFileName)
        {
            StringBuilder builder = new StringBuilder();
            switch (vangurdCommand)
            {
                case VangurdCommand.Collect:
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "collect /session:{0}  /output:\"{1}\"",
                        sessionName,
                        outputName);
                    if (!string.IsNullOrEmpty(configurationFileName))
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, " /config:\"{0}\"", configurationFileName);
                    }

                    break;
                case VangurdCommand.Shutdown:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "shutdown /session:{0}", sessionName);
                    break;
            }

            EqtTrace.Info("VangurdCommandBuilder.GenerateCommandLine: Created the vangurdCommand: {0}", builder);
            return builder.ToString();
        }
    }
}