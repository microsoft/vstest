// -----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

namespace Microsoft.TestPlatform.COM
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Common COM Operations
    /// </summary>
    internal static class ComOperations
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint DllGetClassObjectDelegate(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object ppv);

        /// <summary>
        /// Load a COM object from the supplied assembly using the supplied class ID and interface ID.
        /// </summary>
        /// <param name="pathToNativeAssembly">Path to assembly</param>
        /// <param name="clsid">Class ID</param>
        /// <param name="iid">Interface ID</param>
        /// <returns>Loaded COM instance</returns>
        public static object CreateInstanceFrom(
            string pathToNativeAssembly,
            Guid clsid,
            Guid iid)
        {
            if (string.IsNullOrEmpty(pathToNativeAssembly))
            {
                throw new ArgumentNullException(nameof(pathToNativeAssembly));
            }

            IntPtr nativeDllPointer = NativeMethods.LoadLibraryEx(
                                        pathToNativeAssembly,
                                        IntPtr.Zero,
                                        NativeMethods.LoadLibraryFlags.LOAD_WITH_ALTERED_SEARCH_PATH);

            if (nativeDllPointer == IntPtr.Zero)
            {
                throw new DllNotFoundException(pathToNativeAssembly);
            }

            IntPtr dllGetClassObjectFunctionPointer = NativeMethods.GetProcAddress(nativeDllPointer, "DllGetClassObject");
            if (dllGetClassObjectFunctionPointer == IntPtr.Zero)
            {
                throw new NullReferenceException("DllGetClassObject");
            }

#pragma warning disable 618
            DllGetClassObjectDelegate dllGetClassObject = (DllGetClassObjectDelegate)Marshal.GetDelegateForFunctionPointer(
#pragma warning restore 618
                dllGetClassObjectFunctionPointer,
                typeof(DllGetClassObjectDelegate));

            object ppv = null;
            object instance = null;
            dllGetClassObject(clsid, typeof(IClassFactory).GetTypeInfo().GUID, out ppv);

            IClassFactory factory = ppv as IClassFactory;
            if (factory != null)
            {
                instance = factory.CreateInstance(null, iid);
                Marshal.ReleaseComObject(factory);
            }

            return instance;
        }

        /// <summary>
        /// P/Invoke methods
        /// </summary>
        private static class NativeMethods
        {
            [Flags]
            public enum LoadLibraryFlags
            {
                /// <summary>
                /// See enum remarks
                /// </summary>
                LOAD_WITH_ALTERED_SEARCH_PATH = 0x8
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)]string procname);
        }
    }
}
