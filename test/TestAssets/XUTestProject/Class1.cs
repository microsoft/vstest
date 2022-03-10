// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE1006 // Naming Styles
namespace xUnitTestProject
#pragma warning restore IDE1006 // Naming Styles
{
    using Xunit;

    public class Class1
    {
        [Fact]
        public void PassTestMethod1()
        {
            Assert.True(true);
        }

        [Fact]
        public void FailTestMethod1()
        {
            Assert.True(false);
        }
    }
}
