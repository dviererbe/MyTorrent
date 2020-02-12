using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyTorrent.HashingServiceProviders
{
    /// <summary>
    /// Provides the service to hash data and return it's <see cref="string"/> representation.
    /// </summary>
    public interface IHashingServiceProvider
    {
        /// <summary>
        /// The length of the <see langword="string"/> representation of an hash value. 
        /// </summary>
        public int HashValueLength { get; }

        /// <summary>
        /// Gets the underlying <see langword="string"/> representation of the algorithm name.
        /// </summary>
        public string AlgorithmName { get; }

        /// <summary>
        /// Computes the hash value for the specified byte array.
        /// </summary>
        /// <param name="buffer">
        /// The input to compute the hash value for.
        /// </param>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="buffer"/> is null.
        /// </exception>
        string ComputeHash(byte[] buffer);

        /// <summary>
        /// Computes the hash value for the specified region of the specified byte array.
        /// </summary>
        /// <param name="buffer">
        /// The input to compute the hash value for.
        /// </param>
        /// <param name="offset">
        /// The offset into the byte array from which to begin using data.
        /// </param>
        /// <param name="count">
        /// The number of bytes in the array to use as data.
        /// </param>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws if <paramref name="offset"/> is out of range. This parameter requires a non-negative number.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws if <paramref name="count"/> is an invalid value. -or- <paramref name="buffer"/> length is invalid.
        /// </exception>
        string ComputeHash(byte[] buffer, int offset, int count);

        /// <summary>
        /// Computes the hash value for the specified <see cref="Stream"/> object.
        /// </summary>
        /// <param name="stream">
        /// The input to compute the hash code for.
        /// </param>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        string ComputeHash(Stream stream);

        /// <summary>
        /// Gets an <see cref="IHashingServiceProvider"/> to incrementaly calculate a hash value.
        /// </summary>
        /// <returns>
        /// An <see cref="IIncrementalHashCalculator"/> implementation that calculates the hash the same way as this <see cref="IHashingServiceProvider"/>.
        /// </returns>
        IIncrementalHashCalculator GetIncrementalHashCalculator();

        /// <summary>
        /// Converts a hash value to a normalized format.
        /// </summary>
        /// <param name="hash">
        /// The non-normalized hash value to normalize.
        /// </param>
        /// <returns>
        /// The normalized hash value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="hash"/> is <see langword="null"/>.
        /// </exception>
        string Normalize(string hash);

        /// <summary>
        /// Checks if a hash value is in a valid hash format.
        /// </summary>
        /// <remarks>
        /// This includes non-normalized hashed.
        /// </remarks>
        /// <param name="hash">
        /// The hash value to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a hash value is in a valid hash format; otherwise <see langword="false"/>
        /// </returns>
        bool Validate(string hash);
    }
}
