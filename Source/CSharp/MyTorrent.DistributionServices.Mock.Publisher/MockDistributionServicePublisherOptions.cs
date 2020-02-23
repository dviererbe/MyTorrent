using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Configuration options for a <see cref="MockDistributionServicePublisher"/> instance.
    /// </summary>
    public class MockDistributionServicePublisherOptions
    {
        /// <summary>
        /// Default options for the <see cref="MockDistributionServicePublisher"/>.
        /// </summary>
        public static readonly MockDistributionServicePublisherOptions Default = new MockDistributionServicePublisherOptions();

        /// <summary>
        /// The maximum size a fragment is allowed to have.
        /// </summary>
        /// <remarks>
        /// All fragments, expect the last fragment of an fragmented file have to be exactly of this size.
        /// But the last fragment must not be empty.
        /// </remarks>
        public long FragmentSize { get; set; } = 1024;
    }
}
