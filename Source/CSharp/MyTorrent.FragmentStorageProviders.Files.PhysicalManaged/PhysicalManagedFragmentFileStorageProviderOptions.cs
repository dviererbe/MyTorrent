using System;
using System.IO;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Configuration options for a storage provider that stores fragments in files.
    /// </summary>
    public class PhysicalManagedFragmentFileStorageProviderOptions
    {
        /// <summary>
        /// Default options for the <see cref="PhysicalManagedFragmentFileStorageProvider"/>.
        /// </summary>
        public static readonly PhysicalManagedFragmentFileStorageProviderOptions Default = CreateDefault();

        /// <summary>
        /// Creates the default options for the <see cref="PhysicalManagedFragmentFileStorageProvider"/>.
        /// </summary>
        /// <returns>
        /// The default options for the <see cref="PhysicalManagedFragmentFileStorageProvider"/>.
        /// </returns>
        private static PhysicalManagedFragmentFileStorageProviderOptions CreateDefault()
        {
            string storageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "fragments");

            long storageSpaceUsageLimit = Environment.WorkingSet;

            if (storageSpaceUsageLimit < 1_000_000L)
                storageSpaceUsageLimit /= 10L;
            else
                storageSpaceUsageLimit = 1_000_000L;

            return new PhysicalManagedFragmentFileStorageProviderOptions(storageFolderPath, storageSpaceUsageLimit, false);
        }

        /// <summary>
        /// Initializes new <see cref="PhysicalManagedFragmentFileStorageProviderOptions"/> instance with default options.
        /// </summary>
        public PhysicalManagedFragmentFileStorageProviderOptions()
        {
            StorageFolderPath = Default.StorageFolderPath;
            StorageSpaceUsageLimit = Default.StorageSpaceUsageLimit;
            ResetOnStartup = Default.ResetOnStartup;
        }

        /// <summary>
        /// Initializes new <see cref="PhysicalManagedFragmentFileStorageProviderOptions"/> instance with custom options.
        /// </summary>
        /// <param name="storageFolderPath">
        /// The Path to the Folder where the fragments are stored.
        /// </param>
        /// <param name="storageSpaceUsageLimit">
        /// Limits how many bytes the storage provider is allowed to store on the filesystem.
        /// </param>
        /// <param name="resetOnStartup">
        /// <see langword="true"/> if the storage provider should reset the storage folder to it's initial state on startup; otherwise <see langword="false"/>.
        /// </param>
        public PhysicalManagedFragmentFileStorageProviderOptions(string storageFolderPath, long storageSpaceUsageLimit, bool resetOnStartup)
        {
            StorageFolderPath = storageFolderPath;
            StorageSpaceUsageLimit = storageSpaceUsageLimit;
            ResetOnStartup = resetOnStartup;
        }

        /// <summary>
        /// The Path to the Folder where the fragments are stored.
        /// </summary>
        public string? StorageFolderPath { get; set; }

        /// <summary>
        /// Limits how many bytes the storage provider is allowed to store on the filesystem.
        /// </summary>
        public long StorageSpaceUsageLimit { get; set; }

        /// <summary>
        /// Gets or sets if the storage provider should reset the storage folder to it's initial state on startup.
        /// </summary>
        public bool ResetOnStartup { get; set; }
    }
}