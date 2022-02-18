// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#pragma warning disable IDE1006 // Naming Styles

namespace vstest.ProgrammerTests.Fakes;

internal class EventRecord<T>
{
    public object? Sender { get; }

    public T Data { get; }

    public EventRecord(object? sender, T data)
    {
        Sender = sender;
        Data = data;
    }
}
