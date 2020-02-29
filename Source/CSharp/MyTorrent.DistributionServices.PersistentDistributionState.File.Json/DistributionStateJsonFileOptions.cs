using System;
using System.IO;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    /// <summary>
    /// Configuration options for an <see cref="DistributionStateJsonFile"/> instance.
    /// </summary>
    public class DistributionStateJsonFileOptions
    {
        /// <summary>
        /// Default options for the <see cref="DistributionStateJsonFileOptions"/>.
        /// </summary>
        public static readonly DistributionStateJsonFileOptions Default = new DistributionStateJsonFileOptions();

        /// <summary>
        /// Gets or sets the path where the json file is stored.
        /// </summary>
        public string? FilePath { get; set; } = null;
    }
}
