using System;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Configuration options for a <see cref="FragmentInMemoryStorageProvider"/> instance.
    /// </summary>
    public class FragmentInMemoryStorageProviderOptions
    {
        /// <summary>
        /// Default options for the <see cref="FragmentInMemoryStorageProvider"/>.
        /// </summary>
        public static readonly FragmentInMemoryStorageProviderOptions Default = CreateDefault();


        /// <summary>
        /// Creates the default options for the <see cref="FragmentInMemoryStorageProvider"/>.
        /// </summary>
        /// <returns>
        /// The default options for the <see cref="FragmentInMemoryStorageProvider"/>.
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

        /// <summary>
        /// Initializes new <see cref="FragmentInMemoryStorageProviderOptions"/> instance with default options.
        /// </summary>
        public FragmentInMemoryStorageProviderOptions()
        {
            StorageSpaceUsageLimit = Default.StorageSpaceUsageLimit;
        }

        /// <summary>
        /// Initializes new <see cref="FragmentInMemoryStorageProviderOptions"/> instance with custom options.
        /// </summary>
        /// <param name="storageSpaceUsageLimit">
        /// Limits how many bytes the <see cref="FragmentInMemoryStorageProvider"/> is allowed to store in the working memory.
        /// </param>
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