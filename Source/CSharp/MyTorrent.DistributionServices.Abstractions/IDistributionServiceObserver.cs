using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An observer of the distribution network which holds the published information to query it.
    /// </summary>
    public interface IDistributionServiceObserver : IDisposable
    {
        /// <summary>
        /// Gets the size of a whole fragment in bytes.
        /// </summary>
        /// <remarks>
        /// No Fragment is allowed to be larger than this <see cref="FragmentSize"/>, but 
        /// the last fragment of an file is allowed to be smaller, but not empty.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceObserver"/> was disposed.
        /// </exception>
        public long FragmentSize { get; }

        /// <summary>
        /// Gets the uris of the endpoints where the fragments are distributed to an d can be retrived from.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceObserver"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints { get; }

        /// <summary>
        /// Checks if the distribution network contains a file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the file to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network conatins a file with the 
        /// specified <paramref name="fileHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceObserver"/> was disposed.
        /// </exception>
        public bool ExistsFile(string fileHash);

        /// <summary>
        /// Checks if the distribution network contains a fragment with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the fragment to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network conatins a fragment with the 
        /// specified <paramref name="fragmentHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceObserver"/> was disposed.
        /// </exception>
        public bool ExistsFragment(string fragmentHash);

        /// <summary>
        /// Tries to get the information about an fragmented file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the content of the file to get the information for.
        /// </param>
        /// <param name="fileInfo">
        /// When this method returns, contains the <see cref="IFragmentedFileInfo"/> about the file with the
        /// specified <paramref name="fileHash"/>, if the operation succeeded, or <see langword="null"/> if the 
        /// operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The operation succeeds if information about the specified file was published to the distribution network.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceObserver"/> was disposed.
        /// </exception>
        public bool TryGetFileInfo(string fileHash, [NotNullWhen(returnValue: true)] out IFragmentedFileInfo? fileInfo);

        /// <summary>
        /// Tries to get the uris of an fragment with a specific hash value where the fragment was distributed to.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the content of the fragment to get the information for.
        /// </param>
        /// <param name="fragmentUris">
        /// When this method returns, contains the enumeration of uris where the fragment with the 
        /// specified <paramref name="fragmentHash"/> was distributed to, if the operation succeeded, or 
        /// an empty array if the operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The operation succeeds if information about the specified fragment was published to the distribution network.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceObserver"/> was disposed.
        /// </exception>
        public bool TryGetFragmentDistribution(string fragmentHash, out IEnumerable<Uri> fragmentUris);
    }
}
