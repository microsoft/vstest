// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform;
using Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("� Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyProduct("Microsoft.TestPlatform.Extensions.TrxLogger")]
[assembly: AssemblyTrademark("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("60d876ee-f278-4bf8-bc8a-15b356895c6f")]

[assembly: TestExtensionTypes(typeof(TrxLogger))]