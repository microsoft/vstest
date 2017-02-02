// Copyright (c) Microsoft. All rights reserved.

namespace xUnitTestProject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
