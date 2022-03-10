// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[AttributeUsage(AttributeTargets.Method)]
public abstract class TestDataSource<T1> : Attribute, ITestDataSource where T1 : notnull
{
    private readonly List<object[]> _data = new();

    public abstract void CreateData(MethodInfo methodInfo);

    public void AddData(T1 value1)
    {
        _data.Add(new object[] { value1 });
    }

    public virtual string GetDisplayName(MethodInfo methodInfo, T1 value1)
    {
        return $"{methodInfo.Name} ({value1})";
    }

    IEnumerable<object[]> ITestDataSource.GetData(MethodInfo methodInfo)
    {
        CreateData(methodInfo);
        return _data;
    }

    string ITestDataSource.GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return GetDisplayName(methodInfo, (T1)data[0]);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public abstract class TestDataSource<T1, T2> : Attribute, ITestDataSource
    where T1 : notnull
    where T2 : notnull
{
    private readonly List<object[]> _data = new();

    public abstract void CreateData(MethodInfo methodInfo);

    public void AddData(T1 value1, T2 value2)
    {
        _data.Add(new object[] { value1, value2 });
    }

    public virtual string GetDisplayName(MethodInfo methodInfo, T1 value1, T2 value2)
    {
        return $"{methodInfo.Name} ({value1}, {value2})";
    }

    IEnumerable<object[]> ITestDataSource.GetData(MethodInfo methodInfo)
    {
        CreateData(methodInfo);
        return _data;
    }

    string ITestDataSource.GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return GetDisplayName(methodInfo, (T1)data[0], (T2)data[1]);
    }
}

