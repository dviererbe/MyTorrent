using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Data structure to keep track how the fragments are distributed in a distribution network.
    /// </summary>
    public class DistributionMap
    {
        private readonly HashSet<Uri> _endpoints;
        
        private readonly Dictionary<string, Client> _clients;
        private readonly Dictionary<string, FragmentedFileInfo> _files;
        private readonly Dictionary<string, FragmentInfo> _fragments;

        private readonly List<Client> _clientsSortedByDistributionRelevance;

        /// <summary>
        /// Occurs when a new <see cref="FragmentedFileInfo"/> was added to this <see cref="DistributionMap"/>.
        /// </summary>
        public event Action<FragmentedFileInfo>? OnNewFragmentedFileInfoAdded;

        private readonly object _writeLock = new object();
        private readonly object _readLock = new object();

        /// <summary>
        /// Initializes a new <see cref="DistributionMap"/> instance.
        /// </summary>
        public DistributionMap()
        {
            _endpoints = new HashSet<Uri>();
            _clients = new Dictionary<string, Client>();
            _files = new Dictionary<string, FragmentedFileInfo>();
            _fragments = new Dictionary<string, FragmentInfo>();
            
            _clientsSortedByDistributionRelevance = new List<Client>();
        }

        /// <summary>
        /// Gets the uris of all client endpoints this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<Uri> Endpoints => _endpoints;

        /// <summary>
        /// Gets the identifiers of the clients this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<IClient> Clients => _clients.Values;

        /// <summary>
        /// Gets the hash values of the fragmented file this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<string> Files => _files.Keys;

        /// <summary>
        /// Gets the hash values of the fragments this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<string> Fragments => _fragments.Keys;

        /// <summary>
        /// Checks if the distribution map contains a client with a specific identifier.
        /// </summary>
        /// <param name="id">
        /// The identifier of the client to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map conatins a client with the 
        /// specified <paramref name="id"/>; otherwise <see langword="false"/>.
        /// </returns>
        public bool ContainsClient(string id)
        {
            lock (this)
            {
                return _clients.ContainsKey(id); 
            }
        }

        /// <summary>
        /// Checks if the distribution map contains a fragmented file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the fragmented file to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map conatins a fragmented file with the 
        /// specified <paramref name="fileHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        public bool ContainsFile(string fileHash)
        {
            lock (_readLock)
            {
                return _files.ContainsKey(fileHash); 
            }
        }

        /// <summary>
        /// Checks if the distribution map contains a fragment with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the fragment to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map conatins a fragment with the 
        /// specified <paramref name="fragmentHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        public bool ContainsFragment(string fragmentHash)
        {
            lock (_readLock)
            {
                return _fragments.ContainsKey(fragmentHash); 
            }
        }

        /// <summary>
        /// Tries to get the information about an fragmented file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the content of the file to get the information for.
        /// </param>
        /// <param name="fileInfo">
        /// When this method returns, contains the <see cref="FragmentedFileInfo"/> about the file with the
        /// specified <paramref name="fileHash"/>, if the operation succeeded, or <see langword="null"/> if the 
        /// operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map contains a fragment with 
        /// the specified <paramref name="fileHash"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetFragmentedFileInfo(string fileHash, [NotNullWhen(returnValue: true)] out FragmentedFileInfo? fileInfo)
        {
            lock (_readLock)
            {
                return _files.TryGetValue(fileHash, out fileInfo); 
            }
        }

        /// <summary>
        /// Tries to get all clients that stores a fragment with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the fragment to search for.
        /// </param>
        /// <param name="clients">
        /// When this method returns, contains the enumeration of uris where the fragment with the 
        /// specified <paramref name="fragmentHash"/> was distributed to, if the operation succeeded, or 
        /// an empty array if the operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map contains a fragment with 
        /// the specified <paramref name="fragmentHash"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetClientsWithFragment(string fragmentHash, out IEnumerable<IClient> clients)
        {
            lock (_readLock)
            {
                if (_fragments.TryGetValue(fragmentHash, out FragmentInfo fragmentInfo))
                {
                    var clientArray = new Client[fragmentInfo.FragmentOwner.Count];
                    fragmentInfo.FragmentOwner.CopyTo(clientArray);

                    clients = clientArray;
                    return true;
                }
            }

            clients = Array.Empty<IClient>();
            return false;
        }

        /// <summary>
        /// Removes all keys an values from the <see cref="DistributionMap"/>.
        /// </summary>
        public void Clear()
        {
            lock (_writeLock)
            {
                lock (_readLock)
                {
                    _clients.Clear();
                    _clientsSortedByDistributionRelevance.Clear();
                    _endpoints.Clear();
                    _fragments.Clear();
                    _files.Clear();
                }
            }
        }
    }
}
