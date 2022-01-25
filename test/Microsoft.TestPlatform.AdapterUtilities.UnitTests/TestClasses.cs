// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestClasses;

#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Collections.Generic;

internal class Outer
{
    public void Method0() { }
    public void Method1(int i) { }
    public void Method2(List<string> ls) { }
    public void Method3(string p, int l) { }
    internal class Inner
    {
        public void Method0() { }
        public void Method1(int i) { }
        public void Method2<TU>(int i) { }
        public void Method3<TU, T>(int i) { }
    }
}

internal class OuterPrime : Outer { }

internal class Outer<T>
{
    public void Method0() { }
    public void Method1(T t) { }
    public void Method2<TU>(TU[] u) { }
    public void Method3<TU>(T t, TU u) { }

    internal class Inner<TV>
    {
        public void Method0() { }
        public void Method1(T t) { }
        public void Method2(TV v) { }
        public void Method3<TU>(T t, TU u, TV v) { }
        public void Method4<TU, TX>(TX x, TU u) { }
        public void Method5<TU, TX>(List<TX> x, TU u) { }

        internal class MoreInner<I>
        {
            public void Method0<TU>(T t, TV v, I i, TU u) { }
        }
    }
}

internal class OuterPrime<TZ> : Outer<TZ> { }

internal class OuterPrime<TY, TZ> : Outer<TZ> { }

internal class OuterString : Outer<string> { }

internal interface IImplementation
{
    void ImplMethod0();
    void ImplMethod1(int i);
}

internal class Impl : IImplementation
{
    void IImplementation.ImplMethod0() { }
    void IImplementation.ImplMethod1(int i) { }
}

internal interface IImplementation<T>
{
    void ImplMethod0();
    void ImplMethod1(T t);
    void ImplMethod2<TU>(T t, TU u, string s);
}

internal class Impl<T> : IImplementation<T>
{
    void IImplementation<T>.ImplMethod0() { }
    void IImplementation<T>.ImplMethod1(T t) { }
    void IImplementation<T>.ImplMethod2<TU>(T t, TU u, string s) { }
}

internal class Overloads
{
    public void Overload0() { }
    public void Overload0(int i) { }
    public void Overload0(int i, Overloads c) { }
    public unsafe void Overload0(int* p) { }
    public void Overload0(dynamic d) { }
    public void Overload0<TU>(TU u) { }
    public void Overload0<TU>() { }
    public void Overload0<TU, T>() { }
    public void Overload0<TU>(TU[] u) { }
    public void Overload0<TU>(TU[][] u) { }
    public void Overload0<TU>(TU[,] u) { }
    public void Overload0<TU>(TU[,,] u) { }
    public void Overload0<TU>(List<int> l) { }
    public void Overload0<TU>(List<TU> l) { }
    public void Overload0<TU, TV>(Tuple<TU, TV> t0, Tuple<TV, TU> t1) { }
    public void Overload0(Tuple<Tuple<string[,], int>> t0) { }
    public void Overload0(Tuple<Tuple<string>, Tuple<int>> t) { }
    public void Overload0<TU>(Tuple<Tuple<Outer<TU>.Inner<TU>>> t) { }
}
#pragma warning restore IDE0060 // Remove unused parameter