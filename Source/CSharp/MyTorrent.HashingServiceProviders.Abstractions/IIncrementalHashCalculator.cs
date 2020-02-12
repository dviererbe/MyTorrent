using System;

namespace MyTorrent.HashingServiceProviders
{
    /// <summary>
    /// Incrementaly calculates a hash value.
    /// </summary>
    public interface IIncrementalHashCalculator : IDisposable
    {
        /// <summary>
        /// Appends the specified data to the data already processed in this <see cref="IIncrementalHashCalculator"/>.
        /// </summary>
        /// <param name="data">
        /// The data to process.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        void AppendData(byte[] data);

        /// <summary>
        /// Appends the specified number of bytes from the specified data, starting at the specified offset, to the data already processed in this in this <see cref="IIncrementalHashCalculator"/>.
        /// </summary>
        /// <param name="data">
        /// The data to process.
        /// </param>
        /// <param name="offset">
        /// The offset into the byte array from which to begin using data.
        /// </param>
        /// <param name="count">
        /// The number of bytes to use from <paramref name="data"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws if <paramref name="data"/> or <paramref name="offset"/> is negative. -or- <paramref name="count"/> is larger than the length of <paramref name="data"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws if the sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the length of <paramref name="data"/>.
        /// </exception>
        void AppendData(byte[] data, int offset, int count);

        /// <summary>
        /// Retrieves the hash value for the data accumulated from prior calls to the <seealso cref="AppendData(byte[])"/> and/or <seealso cref="AppendData(byte[], int, int)"/> methods, and resets the object to its initial state.
        /// </summary>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        string GetHashAndReset();
    }
}
