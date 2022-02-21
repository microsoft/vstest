// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Intent;

using System.Reflection;

public interface IRunLogger
{
    void WriteTestPassed(MethodInfo m);
    void WriteTestInconclusive(MethodInfo m);
    void WriteTestFailure(MethodInfo m, Exception ex);
    void WriteFrameworkError(Exception ex);
}
