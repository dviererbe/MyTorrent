using System;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Configuration options for a <see cref="FragmentInMemoryStorageProvider"/> instance.
    /// </summary>
    public class FragmentInMemoryStorageProviderOptions
    {
        public static readonly FragmentInMemoryStorageProviderOptions Default = CreateDefault();


        /// <summary>
        /// TODO: DOCUMENT "private static FragmentInMemoryStorageProviderOptions CreateDefault()"
        /// </summary>
        /// <returns>
        ///
        /// </returns>
        private static FragmentInMemoryStorageProviderOptions CreateDefault()
        {
            long usageLimit = Environment.WorkingSet;

            if (usageLimit < 1_000_000L)
                usageLimit /= 10L;
            else
                usageLimit = 1_000_000L;

            return new FragmentInMemoryStorageProviderOptions(usageLimit);
        }

        public FragmentInMemoryStorageProviderOptions()
        {
            StorageSpaceUsageLimit = Default.StorageSpaceUsageLimit;
        }

        public FragmentInMemoryStorageProviderOptions(long storageSpaceUsageLimit)
        {
            StorageSpaceUsageLimit = storageSpaceUsageLimit;
        }

        /// <summary>
        /// Limits how many bytes the <see cref="FragmentInMemoryStorageProvider"/> is allowed to store in the working memory.
        /// </summary>
        public long StorageSpaceUsageLimit { get; set; }

        //Feature-Proposal: ValidateWrittenData
        /* 
        /// <summary>
        /// Validate that the fragment data matches the corresponding specified hash if a fragment is added to the <see cref="FragmentInMemoryStorageProvider"/>.
        /// </summary>
        public bool ValidateWrittenData { get; set; } = false;
        */
    }
}