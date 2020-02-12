using System;
using System.IO;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Configuration options for a storage provider that stores fragments in files.
    /// </summary>
    public class FragmentFileStorageProviderOptions
    {
        public static readonly FragmentFileStorageProviderOptions Default = CreateDefault();

        /// <summary>
        /// TODO: DOCUMENT "private static FragmentFileStorageProviderOptions CreateDefault()"
        /// </summary>
        /// <returns>
        ///
        /// </returns>
        private static FragmentFileStorageProviderOptions CreateDefault()
        {
            string storageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "fragments");

            long storageSpaceUsageLimit = Environment.WorkingSet;

            if (storageSpaceUsageLimit < 1_000_000L)
                storageSpaceUsageLimit /= 10L;
            else
                storageSpaceUsageLimit = 1_000_000L;

            return new FragmentFileStorageProviderOptions(storageFolderPath, storageSpaceUsageLimit, false);
        }

        public FragmentFileStorageProviderOptions()
        {
            StorageFolderPath = Default.StorageFolderPath;
            StorageSpaceUsageLimit = Default.StorageSpaceUsageLimit;
            ResetOnStartup = Default.ResetOnStartup;
        }

        public FragmentFileStorageProviderOptions(string storageFolderPath, long storageSpaceUsageLimit, bool resetOnStartup)
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