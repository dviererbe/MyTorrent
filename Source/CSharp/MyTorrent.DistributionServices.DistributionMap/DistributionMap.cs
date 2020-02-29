using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Data structure to keep track how the fragments are distributed in a distribution network.
    /// </summary>
    public class DistributionMap
    {
        private readonly HashSet<Uri> _endpoints;
        
        private readonly Dictionary<string, ClientInfo> _clients;
        private readonly Dictionary<string, FragmentedFileInfo> _files;
        private readonly Dictionary<string, FragmentInfo> _fragments;

        private readonly List<ClientInfo> _clientsSortedByDistributionRelevance;

        /// <summary>
        /// Occurs when a new <see cref="FragmentedFileInfo"/> was added to this <see cref="DistributionMap"/>.
        /// </summary>
        public event Action<FragmentedFileInfo>? OnNewFragmentedFileInfoAdded;

        /// <summary>
        /// Initializes a new <see cref="DistributionMap"/> instance.
        /// </summary>
        public DistributionMap()
        {
            _endpoints = new HashSet<Uri>();
            _clients = new Dictionary<string, ClientInfo>();
            _files = new Dictionary<string, FragmentedFileInfo>();
            _fragments = new Dictionary<string, FragmentInfo>();
            
            _clientsSortedByDistributionRelevance = new List<ClientInfo>();
        }

        /// <summary>
        /// Gets the uris of all client endpoints this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<Uri> Endpoints => _endpoints;

        /// <summary>
        /// Gets the identifiers of the clients this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<IClientInfo> Clients => _clients.Values;

        /// <summary>
        /// Gets the hash values of the fragmented file this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<FragmentedFileInfo> Files => _files.Values;

        /// <summary>
        /// Gets the hash values of the fragments this <see cref="DistributionMap"/> contains.
        /// </summary>
        public IReadOnlyCollection<IFragmentInfo> Fragments => _fragments.Values;

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
            return _clients.ContainsKey(id);
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
            return _files.ContainsKey(fileHash);
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
            return _fragments.ContainsKey(fragmentHash);
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
            return _files.TryGetValue(fileHash, out fileInfo);     
        }

        /// <summary>
        /// Tries to get the information about an fragment with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the content of the fragment to get the information for.
        /// </param>
        /// <param name="fragmentInfo">
        /// When this method returns, contains the <see cref="FragmentInfo"/> about the fragment with the
        /// specified <paramref name="fragmentHash"/>, if the operation succeeded, or <see langword="null"/> if the 
        /// operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map contains a fragment with 
        /// the specified <paramref name="fragmentHash"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetFragmentInfo(string fragmentHash, [NotNullWhen(returnValue: true)] out IFragmentInfo? fragmentInfo)
        {
            if (_fragments.TryGetValue(fragmentHash, out FragmentInfo info))
            {
                fragmentInfo = info;
                return true;
            }

            fragmentInfo = null;
            return false;
        }

        /// <summary>
        /// Tries to get the information about an client with a specific identifier.
        /// </summary>
        /// <param name="clientId">
        /// The identifier of the client to get the information for.
        /// </param>
        /// <param name="clientInfo">
        /// When this method returns, contains the <see cref="ClientInfo"/> about the client with the
        /// specified <paramref name="clientId"/>, if the operation succeeded, or <see langword="null"/> if the 
        /// operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution map contains a client with 
        /// the specified <paramref name="clientId"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetClientInfo(string clientId, [NotNullWhen(returnValue: true)] out IClientInfo? clientInfo)
        {
            if (_clients.TryGetValue(clientId, out ClientInfo info))
            {
                clientInfo = info;
                return true;
            }

            clientInfo = null;
            return false;
        }

        public bool TryAddFileInfo(string fileHash, long fileSize, IEnumerable<string> fragmentHashSequence)
        {
            return _files.TryAdd(fileHash, new FragmentedFileInfo(fileHash, fileSize, fragmentHashSequence));
        }

        public bool TryAddFileInfo(IFragmentedFileInfo fileInfo)
        {
            if (fileInfo is FragmentedFileInfo fragmentedFileInfo)
                return _files.TryAdd(fileInfo.Hash, fragmentedFileInfo);
            else
                return _files.TryAdd(fileInfo.Hash, new FragmentedFileInfo(fileInfo.Hash, fileInfo.Size, fileInfo.FragmentSequence));
        }

        public bool TryAddFileInfo(FragmentedFileInfo fileInfo)
        {
            return _files.TryAdd(fileInfo.Hash, fileInfo);
        }
        
        public bool TryAddFragmentInfo(string fragmentHash, long fragmentSize)
        {
            return _fragments.TryAdd(fragmentHash, new FragmentInfo(fragmentHash, fragmentSize));
        }

        public bool TryAddFragmentInfo(FragmentInfo fragmentInfo)
        {
            return _fragments.TryAdd(fragmentInfo.Hash, fragmentInfo);
        }

        public bool TryAddClient(string clientIdentifier, IEnumerable<Uri> endpoints, IEnumerable<string> storedFragments)
        {
            if (_clients.ContainsKey(clientIdentifier))
                return false;
            
            if (_endpoints.Overlaps(endpoints))
                return false;

            HashSet<string> fragments = new HashSet<string>(storedFragments);

            ClientInfo clientInfo = new ClientInfo(clientIdentifier, endpoints, fragments);

            foreach (string fragmentHash in fragments)
            {
                if (fragments.Add(fragmentHash) && _fragments.TryGetValue(fragmentHash, out FragmentInfo fragmentInfo))
                {
                    fragmentInfo._fragmentOwner.Add(clientIdentifier, clientInfo);
                }
            }

            _clients.Add(clientIdentifier, clientInfo);

            foreach (Uri endpoint in endpoints)
            {
                _endpoints.Add(endpoint);
            }

            return true;
        }

        public void TryAddFragmentToClient(string fragmentHash, string clientId)
        {
            if (_fragments.TryGetValue(fragmentHash, out FragmentInfo fragmentInfo))
            {
                if (_clients.TryGetValue(clientId, out ClientInfo clientInfo))
                {
                    fragmentInfo._fragmentOwner.TryAdd(clientId, clientInfo);
                }
            }
        }

        public void TryAddFragmentToClients(string fragmentHash, IEnumerable<string> clients)
        {
            if (_fragments.TryGetValue(fragmentHash, out FragmentInfo fragmentInfo))
            {
                foreach (string clientId in clients)
                {
                    if (_clients.TryGetValue(clientId, out ClientInfo clientInfo))
                    {
                        fragmentInfo._fragmentOwner.TryAdd(clientId, clientInfo);
                    }
                }
            }
        }

        public bool OverlapsEntpoint(IEnumerable<Uri> uris)
        {
            return _endpoints.Overlaps(uris);
        }

        public bool RemoveFile(string fileHash)
        {
            return _files.Remove(fileHash);
        }

        public bool RemoveFragment(string fragmentHash)
        {
            if (_fragments.TryGetValue(fragmentHash, out FragmentInfo fragmentInfo))
            {
                foreach (ClientInfo fragmentOwner in fragmentInfo.FragmentOwner)
                {
                    fragmentOwner.RemoveFragment(fragmentHash);
                }

                _fragments.Remove(fragmentHash);
            }

            return false;
        }

        public bool RemoveClient(string clientId)
        {
            if (_clients.TryGetValue(clientId, out ClientInfo clientInfo))
            {
                foreach (string fragmentHash in clientInfo.Fragments)
                {
                    if (_fragments.TryGetValue(fragmentHash, out FragmentInfo fragmentInfo))
                    {
                        fragmentInfo._fragmentOwner.Remove(clientId);

                        if (fragmentInfo._fragmentOwner.Count == 0)
                            _fragments.Remove(fragmentHash);
                    }
                }

                _endpoints.ExceptWith(clientInfo.Endpoints);

                _clients.Remove(clientId);
                _clientsSortedByDistributionRelevance.Remove(clientInfo);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all keys an values from the <see cref="DistributionMap"/>.
        /// </summary>
        public void Clear()
        {
            _clients.Clear();
            _clientsSortedByDistributionRelevance.Clear();
            _endpoints.Clear();
            _fragments.Clear();
            _files.Clear();
        }
    }
}
