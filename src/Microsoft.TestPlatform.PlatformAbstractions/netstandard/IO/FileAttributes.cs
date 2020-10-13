// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD1_0

namespace System.IO
{
    /// <summary>
    /// Provides attributes for files and directories.
    /// </summary>
    [Flags]
    public enum FileAttributes
    {
        /// <summary>
        /// The file is a candidate for backup or removal.
        /// </summary>
        Archive = 0x20,

        /// <summary>
        /// The file is compressed.
        /// </summary>
        Compressed = 0x800,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Device = 0x40,

        /// <summary>
        /// The file is a directory.
        /// </summary>
        Directory = 0x10,

        /// <summary>
        /// The file or directory is encrypted. For a file, this means that all data in the
        /// file is encrypted. For a directory, this means that encryption is the default
        /// for newly created files and directories.
        /// </summary>
        Encrypted = 0x4000,

        /// <summary>
        /// The file is hidden, and thus is not included in an ordinary directory listing.
        /// </summary>
        Hidden = 0x2,

        /// <summary>
        /// The file or directory includes data integrity support. When this value is applied
        /// to a file, all data streams in the file have integrity support. When this value
        /// is applied to a directory, all new files and subdirectories within that directory,
        /// by default, include integrity support.
        /// </summary>
        IntegrityStream = 0x8000,

        /// <summary>
        /// The file is a standard file that has no special attributes. This attribute is
        /// valid only if it is used alone.
        /// </summary>
        Normal = 0x80,

        /// <summary>
        /// The file or directory is excluded from the data integrity scan. When this value
        /// is applied to a directory, by default, all new files and subdirectories within
        /// that directory are excluded from data integrity.
        /// </summary>
        NoScrubData = 0x20000,

        /// <summary>
        /// The file will not be indexed by the operating system's content indexing service.
        /// </summary>
        NotContentIndexed = 0x2000,

        /// <summary>
        /// The file is offline. The data of the file is not immediately available.
        /// </summary>
        Offline = 0x1000,

        /// <summary>
        /// The file is read-only.
        /// </summary>
        ReadOnly = 0x1,

        /// <summary>
        /// The file contains a reparse point, which is a block of user-defined data associated
        /// with a file or a directory.
        /// </summary>
        ReparsePoint = 0x400,

        /// <summary>
        /// The file is a sparse file. Sparse files are typically large files whose data
        /// consists of mostly zeros.
        /// </summary>
        SparseFile = 0x200,

        /// <summary>
        /// The file is a system file. That is, the file is part of the operating system
        /// or is used exclusively by the operating system.
        /// </summary>
        System = 0x4,

        /// <summary>
        /// The file is temporary. A temporary file contains data that is needed while an
        /// application is executing but is not needed after the application is finished.
        /// File systems try to keep all the data in memory for quicker access rather than
        /// flushing the data back to mass storage. A temporary file should be deleted by
        /// the application as soon as it is no longer needed.
        /// </summary>
        Temporary = 0x100
    }
}

#endif