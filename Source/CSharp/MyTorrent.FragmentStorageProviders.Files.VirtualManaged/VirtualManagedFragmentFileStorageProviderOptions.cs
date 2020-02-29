using System;
using System.IO;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Configuration options for a storage provider that stores fragments in files.
    /// </summary>
    public class VirtualManagedFragmentFileStorageProviderOptions
    {
        /// <summary>
        /// Default options for the <see cref="VirtualManagedFragmentFileStorageProvider"/>.
        /// </summary>
        public static readonly VirtualManagedFragmentFileStorageProviderOptions Default = CreateDefault();

        /// <summary>
        /// Creates the default options for the <see cref="VirtualManagedFragmentFileStorageProvider"/>.
        /// </summary>
        /// <returns>
        /// The default options for the <see cref="VirtualManagedFragmentFileStorageProvider"/>.
        /// </returns>
        private static VirtualManagedFragmentFileStorageProviderOptions CreateDefault()
        {
            string storageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "fragments");

            long storageSpaceUsageLimit = 1_000_000_000;

            return new VirtualManagedFragmentFileStorageProviderOptions(storageFolderPath, storageSpaceUsageLimit, false);
        }

        /// <summary>
        /// Initializes new <see cref="VirtualManagedFragmentFileStorageProviderOptions"/> instance with default options.
        /// </summary>
        public VirtualManagedFragmentFileStorageProviderOptions()
        {
            StorageFolderPath = Default.StorageFolderPath;
            StorageSpaceUsageLimit = Default.StorageSpaceUsageLimit;
            ResetOnStartup = Default.ResetOnStartup;
        }

        /// <summary>
        /// Initializes new <see cref="VirtualManagedFragmentFileStorageProviderOptions"/> instance with custom options.
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
        public VirtualManagedFragmentFileStorageProviderOptions(string storageFolderPath, long storageSpaceUsageLimit, bool resetOnStartup)
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