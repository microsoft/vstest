// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

// TODO: Make this internal, I am just trying to have easier time trying this out.
public static class TestServiceLocator
{
    public static Dictionary<Type, object> Instances { get; } = new Dictionary<Type, object>();
    public static List<Resolve> Resolves { get; } = new();

    public static void Register<TRegistration>(TRegistration instance)
    {
        Instances.Add(typeof(TRegistration), instance);
    }

    public static TRegistration Get<TRegistration>()
    {
        if (!Instances.TryGetValue(typeof(TRegistration), out var instance))
            throw new InvalidOperationException($"Cannot find instance for type {typeof(TRegistration)}.");

#if !NETSTANDARD1_0
        Resolves.Add(new Resolve(typeof(TRegistration).FullName, Environment.StackTrace));
#endif
        return (TRegistration)instance;
    }
}

// TODO: Make this internal, I am just trying to have easier time trying this out.
public class Resolve
{
    public Resolve(string type, string stackTrace)
    {
        Type = type;
        StackTrace = stackTrace;
    }

    public string Type { get; }
    public string StackTrace { get; }
}
