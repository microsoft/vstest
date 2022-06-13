// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

if (Environment.Is64BitProcess)
{
    throw new InvalidOperationException("Process is supposed to be 32bits.");
}

var envVarName = Environment.GetEnvironmentVariable("DOTNET_ROOT_ENV_VAR_NAME");
if (string.IsNullOrEmpty(envVarName))
{
    throw new InvalidOperationException("Could not find 'DOTNET_ROOT_ENV_VAR_NAME' which is supposed to tell us which DOTNET_ROOTXXX to look up.");
}

var dotnetRootPath = Environment.GetEnvironmentVariable(envVarName);
if (string.IsNullOrEmpty(dotnetRootPath))
{
    throw new InvalidOperationException($"{dotnetRootPath} was not set.");
}

Console.WriteLine("DOTNET_ROOT(x86)={0}", Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)"));
Console.WriteLine("DOTNET_ROOT={0}", Environment.GetEnvironmentVariable("DOTNET_ROOT"));
Console.WriteLine("DOTNET_ROOT_X86={0}", Environment.GetEnvironmentVariable("DOTNET_ROOT_X86"));
Console.WriteLine("DOTNET_ROOT_X64={0}", Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"));

if (!typeof(object).Assembly.Location.StartsWith(dotnetRootPath, StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException($"{typeof(object).Assembly.Location} was not found in {dotnetRootPath}. .NET was not resolved from the correct path.");
}

Console.WriteLine("Process and DOTNET_ROOT* were correctly set.");
