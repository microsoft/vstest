// -----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

namespace Microsoft.TestPlatform.COM
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class factory interface
    /// </summary>
    [ComImport]
    [ComVisible(false)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    internal interface IClassFactory
    {
        /// <summary>
        /// Creates an uninitialized object
        /// </summary>
        /// <param name="outer">
        /// If the object is being created as part of an aggregate, specify a pointer to the controlling IUnknown interface of the aggregate.
        /// Otherwise, this parameter must be NULL.
        /// </param>
        /// <param name="riid">
        /// A reference to the identifier of the interface to be used to communicate with the newly created object. If pUnkOuter is NULL,
        /// this parameter is generally the IID of the initializing interface; if pUnkOuter is non-NULL, riid must be IID_IUnknown.
        /// </param>
        /// <returns>
        /// The address of pointer variable that receives the interface pointer requested in riid. Upon successful return,
        /// *ppvObject contains the requested interface pointer. If the object does not support the interface specified in riid,
        /// the implementation must set *ppvObject to NULL.
        /// </returns>
        [return: MarshalAs(UnmanagedType.Interface)]
        object CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object outer, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        /// <summary>
        /// Locks an object application open in memory. This enables instances to be created more quickly.
        /// </summary>
        /// <param name="fLock">If TRUE, increments the lock count; if FALSE, decrements the lock count.</param>
        void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }
}
