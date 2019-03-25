// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    public interface IConsoleHelper
    {
        int CursorLeft { get; }

        int CursorTop { get; }

        int WindowWidth { get; }

        void SetCursorPosition(int startPos, int cursorTop);
    }
}
