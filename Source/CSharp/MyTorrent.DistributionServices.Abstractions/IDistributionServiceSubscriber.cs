using MyTorrent.FragmentStorageProviders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An subscriber of the distribution network that receives some of the distributed fragments and stores them.
    /// </summary>
    public interface IDistributionServiceSubscriber : IDistributionServiceObserver
    {
        /// <summary>
        /// Gets the <see cref="IFragmentStorageProvider"/> where the fragments are stored which this <see cref="IDistributionServiceSubscriber"/> decided to hold.
        /// </summary>
        /// <remarks>
        /// An <see cref="IDistributionServiceSubscriber"/> of course don't have to hold any fragment that are published by the <see cref="IDistributionServicePublisher"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="IDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public IFragmentStorageProvider FragmentStorage { get; }

        public Task InitializeAsync(IEnumerable<Uri> endpoints);
    }
}
