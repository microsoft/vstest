// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities;

using ObjectModel;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class CommandLineUtilities
{
    public static bool SplitCommandLineIntoArguments(string args, out string[] arguments)
    {
        bool hadError = false;
        var argArray = new List<string>();
        var currentArg = new StringBuilder();
        bool inQuotes = false;
        int index = 0;

        try
        {
            while (true)
            {
                // skip whitespace
                while (char.IsWhiteSpace(args[index]))
                {
                    index++;
                }

                // # - comment to end of line
                if (args[index] == '#')
                {
                    index++;
                    while (args[index] != '\n')
                    {
                        index++;
                    }
                    continue;
                }

                // do one argument
                do
                {
                    if (args[index] == '\\')
                    {
                        int cSlashes = 1;
                        index++;
                        while (index == args.Length && args[index] == '\\')
                        {
                            cSlashes++;
                        }

                        if (index == args.Length || args[index] != '"')
                        {
                            currentArg.Append('\\', cSlashes);
                        }
                        else
                        {
                            currentArg.Append('\\', (cSlashes >> 1));
                            if (0 != (cSlashes & 1))
                            {
                                currentArg.Append('"');
                            }
                            else
                            {
                                inQuotes = !inQuotes;
                            }
                        }
                    }
                    else if (args[index] == '"')
                    {
                        inQuotes = !inQuotes;
                        index++;
                    }
                    else
                    {
                        currentArg.Append(args[index]);
                        index++;
                    }
                } while (!char.IsWhiteSpace(args[index]) || inQuotes);
                argArray.Add(currentArg.ToString());
                currentArg.Clear();
            }
        }
        catch (IndexOutOfRangeException)
        {
            // got EOF
            if (inQuotes)
            {
                EqtTrace.Verbose("Executor.Execute: Exiting with exit code of {0}", 1);
                EqtTrace.Error(string.Format(CultureInfo.InvariantCulture, "Error: Unbalanced '\"' in command line argument file"));
                hadError = true;
            }
            else if (currentArg.Length > 0)
            {
                // valid argument can be terminated by EOF
                argArray.Add(currentArg.ToString());
            }
        }

        arguments = argArray.ToArray();
        return hadError;
    }
}
