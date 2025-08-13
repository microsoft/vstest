// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Hashing;
using System.Text;

namespace Microsoft.TestPlatform.AdapterUtilities;
/// <summary>
/// Used to generate id for tests.
/// </summary>
public class TestIdProvider2
{
    private Guid _id = Guid.Empty;
    private byte[]? _hash;

    private readonly XxHash128 _xxhash;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestIdProvider"/> class.
    /// </summary>
    public TestIdProvider2()
    {
        _xxhash = new XxHash128();
    }

    /// <summary>
    /// Appends a string to id generation seed.
    /// </summary>
    /// <param name="str">String to append to the id seed.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="GetHash"/> or <see cref="GetId"/> is called already.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="str"/> is <see langword="null"/>.</exception>
    public void AppendString(string str)
    {
        if (_hash != null)
        {
            throw new InvalidOperationException(Resources.Resources.ErrorCannotAppendAfterHashCalculation);
        }
        _ = str ?? throw new ArgumentNullException(nameof(str));

        var bytes = Encoding.Unicode.GetBytes(str);

        _xxhash.Append(bytes);
    }

    /// <summary>
    /// Appends an array of bytes to id generation seed.
    /// </summary>
    /// <param name="bytes">Array to append to the id seed.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="GetHash"/> or <see cref="GetId"/> is called already.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
    public void AppendBytes(byte[] bytes)
    {
        if (_hash != null)
        {
            throw new InvalidOperationException(Resources.Resources.ErrorCannotAppendAfterHashCalculation);
        }
        _ = bytes ?? throw new ArgumentNullException(nameof(bytes));

        if (bytes.Length == 0)
        {
            return;
        }

        _xxhash.Append(bytes);
    }

    /// <summary>
    /// Calculates the Id seed.
    /// </summary>
    /// <returns>An array containing the seed.</returns>
    /// <remarks>
    /// <see cref="AppendBytes(byte[])"/> and <see cref="AppendString(string)"/> cannot be called
    /// on instance after this method is called.
    /// </remarks>
    public byte[] GetHash()
    {
        if (_hash != null)
        {
            return _hash;
        }

        // Finalize the hash. We don't have any more data so we provide empty.
        _hash = _xxhash.GetCurrentHash();

        return _hash!;
    }

    /// <summary>
    /// Calculates the Id from the seed.
    /// </summary>
    /// <returns>Id</returns>
    /// <remarks>
    /// <see cref="AppendBytes(byte[])"/> and <see cref="AppendString(string)"/> cannot be called
    /// on instance after this method is called.
    /// </remarks>
    public Guid GetId()
    {
        if (_id != Guid.Empty)
        {
            return _id;
        }

#if NET6_0_OR_GREATER
        var hashSlice = GetHash().AsSpan().Slice(0, 16);
        _id = new Guid(hashSlice);
#else
        // create from span?
        var toGuid = new byte[16];
        Array.Copy(GetHash(), toGuid, 16);
        _id = new Guid(toGuid);
#endif

        return _id;
    }
}
