﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

using System;
using System.Collections.Generic;

// TODO: Make this internal, I am just trying to have easier time trying this out.
public static class TestServiceLocator
{
    public static Dictionary<string, object> Instances { get; } = new Dictionary<string, object>();
    public static List<Resolve> Resolves { get; } = new();

    public static void Register<TRegistration>(string name, TRegistration instance)
    {
        Instances.Add(name, instance);
    }

    public static TRegistration? Get<TRegistration>(string name)
    {
        if (!Instances.TryGetValue(name, out var instance))
        {
            return default;
            // TODO: Add enable flag for the whole provider to activate so I can leverage throwing in programmer tests, but not run into it in Playground, or other debug builds.
            // throw new InvalidOperationException($"Cannot find an instance for name {name}.");
        }

#if !NETSTANDARD1_0
        Resolves.Add(new Resolve(name, typeof(TRegistration).FullName, Environment.StackTrace));
#endif
        return (TRegistration)instance;
    }

    public static void Clear()
    {
        Instances.Clear();
        Resolves.Clear();
    }
}

// TODO: Make this internal, I am just trying to have easier time trying this out.
public class Resolve
{
    public Resolve(string name, string type, string stackTrace)
    {
        Name = name;
        Type = type;
        StackTrace = stackTrace;
    }

    public string Name { get; }
    public string Type { get; }
    public string StackTrace { get; }
}
