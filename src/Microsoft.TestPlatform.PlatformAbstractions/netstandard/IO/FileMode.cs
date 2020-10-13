// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD1_0

namespace System.IO
{
    /// <summary>
    /// Specifies how the operating system should open a file.
    /// </summary>
    public enum FileMode
    {
        /// <summary>
        /// Opens the file if it exists and seeks to the end of the file, or creates a new
        /// file. This requires System.Security.Permissions.FileIOPermissionAccess.Append
        /// permission. FileMode.Append can be used only in conjunction with FileAccess.Write.
        /// Trying to seek to a position before the end of the file throws an System.IO.IOException
        /// exception, and any attempt to read fails and throws a System.NotSupportedException
        /// exception.
        /// </summary>
        Append = 6,

        /// <summary>
        /// Specifies that the operating system should create a new file. If the file already
        /// exists, it will be overwritten. This requires System.Security.Permissions.FileIOPermissionAccess.Write
        /// permission. FileMode.Create is equivalent to requesting that if the file does
        /// not exist, use System.IO.FileMode.CreateNew; otherwise, use System.IO.FileMode.Truncate.
        /// If the file already exists but is a hidden file, an System.UnauthorizedAccessException
        /// exception is thrown.
        /// </summary>
        Create = 2,

        /// <summary>
        /// Specifies that the operating system should create a new file. This requires System.Security.Permissions.FileIOPermissionAccess.Write
        /// permission. If the file already exists, an System.IO.IOException exception is
        /// thrown.
        /// </summary>
        CreateNew = 1,

        /// <summary>
        /// Specifies that the operating system should open an existing file. The ability
        /// to open the file is dependent on the value specified by the System.IO.FileAccess
        /// enumeration. A System.IO.FileNotFoundException exception is thrown if the file
        /// does not exist.
        /// </summary>
        Open = 3,

        /// <summary>
        /// Specifies that the operating system should open a file if it exists; otherwise,
        /// a new file should be created. If the file is opened with FileAccess.Read, System.Security.Permissions.FileIOPermissionAccess.Read
        /// permission is required. If the file access is FileAccess.Write, System.Security.Permissions.FileIOPermissionAccess.Write
        /// permission is required. If the file is opened with FileAccess.ReadWrite, both
        /// System.Security.Permissions.FileIOPermissionAccess.Read and System.Security.Permissions.FileIOPermissionAccess.Write
        /// permissions are required.
        /// </summary>
        OpenOrCreate = 4,

        /// <summary>
        /// Specifies that the operating system should open an existing file. When the file
        /// is opened, it should be truncated so that its size is zero bytes. This requires
        /// System.Security.Permissions.FileIOPermissionAccess.Write permission. Attempts
        /// to read from a file opened with FileMode.Truncate cause an System.ArgumentException
        /// exception.
        /// </summary>
        Truncate = 5
    }
}

#endif