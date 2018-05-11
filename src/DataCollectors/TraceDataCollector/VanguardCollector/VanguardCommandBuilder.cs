// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System.Globalization;
    using System.Text;
    using Interfaces;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// The VanguardCommandBuilder class.
    /// </summary>
    internal class VanguardCommandBuilder : IVanguardCommandBuilder
    {
        /// <inheritdoc />
        public string GenerateCommandLine(
            VanguardCommand vanguardCommand,
            string sessionName,
            string outputName,
            string configurationFileName)
        {
            StringBuilder builder = new StringBuilder();
            switch (vanguardCommand)
            {
                case VanguardCommand.Collect:
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
                case VanguardCommand.Shutdown:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "shutdown /session:{0}", sessionName);
                    break;
            }

            EqtTrace.Info("VanguardCommandBuilder.GenerateCommandLine: Created the vanguardCommand: {0}", builder);
            return builder.ToString();
        }
    }
}