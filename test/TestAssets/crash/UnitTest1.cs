// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE1006 // Naming Styles
namespace timeout
#pragma warning restore IDE1006 // Naming Styles
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var list = new List<a>();
            list.AddRange(Enumerable.Range(0, 100000).Select(a => new a()));
            // stack overflow
            Span<byte> s = stackalloc byte[int.MaxValue];
        }
    }

    public class a
    {
        public static Random random = new Random();
        public a()
        {
            abc = new string(((char)(byte)random.Next(0, 255)), random.Next(10000, 100_000));
        }
        public string abc;
    }
}
