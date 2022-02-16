// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

public class Id
{
    private int _id;

    public Id () : this(0)
    {
    }

    public Id (int firstId)
    {
        _id = firstId;
    }

    public int Next()
    {
        return Interlocked.Increment(ref _id);
    }
}
