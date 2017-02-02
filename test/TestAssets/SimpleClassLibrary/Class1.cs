// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SimpleClassLibrary
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class Class1
    {
        public void PassingTest()
        {
            Debug.Assert(2 == 2);
        }

        public async Task AsyncTestMethod()
        {
            await Task.CompletedTask;
        }

        public void OverLoadedMethod()
        {
        }

        public void OverLoadedMethod(string name)
        {
        }
    }
}
