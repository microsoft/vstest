// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

#if NET46
using System.Security.Cryptography;
#endif

    /// <summary>
    /// Wrapper class for cryptographic hashing.
    /// This class uses SHA1 instead of MD5 in order to conform to the FIPS standard.
    /// </summary>
    public static class EqtHash
    {
        /// <summary>
        /// Calculates a hash of the string and copies the first 128 bits of the hash
        /// to a new Guid.
        /// </summary>
        [SuppressMessage("Microsoft.Cryptographic.Standard", "CA5354:SHA1CannotBeUsed", Justification = "Hash Algorithm is used only to gererate unique testcase id.")]
        public static Guid GuidFromString(string data)
        {
            Debug.Assert(data != null);
#if NET46
            using (HashAlgorithm provider = SHA1.Create())
#else
            using (var provider = System.Security.Cryptography.SHA256.Create())
#endif
            {
                byte[] hash = provider.ComputeHash(System.Text.Encoding.Unicode.GetBytes(data));

                // Guid is always 16 bytes
                Debug.Assert(Guid.Empty.ToByteArray().Length == 16, "Expected Guid to be 16 bytes");

                byte[] toGuid = new byte[16];
                Array.Copy(hash, toGuid, 16);

                return new Guid(toGuid);
            }
        }
    }
}
