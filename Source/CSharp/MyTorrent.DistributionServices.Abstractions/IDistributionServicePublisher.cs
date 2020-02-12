using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An publisher of the distribution service that published information about fragmented files and the fragment data.
    /// </summary>
    public interface IDistributionServicePublisher : IDistributionServiceObserver
    {
        /// <summary>
        /// Asynchronously publishes information about an fragmented file.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the entire file content of the fragmented file whose information should be published.
        /// </param>
        /// <param name="fileSize">
        /// The size of the entire file content of the fragmented file whose information should be published.
        /// </param>
        /// <param name="fragmentHashSequence">
        /// Sequence of hash values of the fragments the file consists of. 
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileHash"/>, <paramref name="fragmentHashSequence"/> or one of 
        /// the elements of <paramref name="fragmentHashSequence"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fileSize"/> is negative or zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An file with the specified <paramref name="fileHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fileHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to publish the file info, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServicePublisher"/> was disposed.
        /// </exception>
        public Task PublishFileInfoAsync(string fileHash, long fileSize, IEnumerable<string> fragmentHashSequence, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Asynchronously distributes a file fragment to the distribution network.
        /// </summary>
        /// <param name="fragmentHash">
        /// Hash value of the file fragment that should be distributed.
        /// </param>
        /// <param name="fragmentData">
        /// Content of the file fragment that should be distributed.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous distrubution operation and wraps the 
        /// uris of the fragment where it was distributed to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fragmentHash"/> or <paramref name="fragmentData"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length of the <paramref name="fragmentData"/> is larger than the maximum allowed <see cref="IDistributionServiceObserver.FragmentSize"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to distribute the fragment, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServicePublisher"/> was disposed.
        /// </exception>
        public Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, byte[] fragmentData, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Asynchronously distributes a file fragment to the distribution network.
        /// </summary>
        /// <param name="fragmentHash">
        /// Hash value of the file fragment that should be distributed.
        /// </param>
        /// <param name="fragmentStream">
        /// Content of the file fragment that should be distributed, that can be read from this <see cref="Stream"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous distrubution operation and wraps the 
        /// uris of the fragment where it was distributed to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fragmentHash"/> or <paramref name="fragmentStream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length of the <paramref name="fragmentStream"/> is larger than the maximum allowed <see cref="IDistributionServiceObserver.FragmentSize"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to distribute the fragment, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServicePublisher"/> was disposed.
        /// </exception>
        public Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, Stream fragmentStream, CancellationToken? cancellationToken = null);
    }
}
