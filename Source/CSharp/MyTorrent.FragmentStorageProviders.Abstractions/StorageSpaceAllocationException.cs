using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Represents errors that occur during storage space (de-)allocation.
    /// </summary>
    public class StorageSpaceAllocationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageSpaceAllocationException"/> class with information about the storage space.
        /// </summary>
        /// <param name="requestedAllocationSize">
        /// Amount of bytes that should be allocated.
        /// </param>
        /// <param name="usedStorageSpace">
        /// How many bytes of the storage space where the resources should be allocated already were used.
        /// </param>
        /// <param name="availableStorageSpace">
        /// How many bytes of the storage space where the resources should be allocated were available.
        /// </param>
        /// <param name="storageSpaceUsageLimit">
        /// The maximum amount of bytes that were allowed to allocate. 
        /// </param>
        /// <param name="allocationTokenId">
        /// The id of the allocation token from which the associated resources should be allocated; <see langword="null" /> if
        /// the common resources of the storage prover should be allocated.
        /// </param>
        /// <remarks>
        /// If <paramref name="allocationTokenId"/> is <see langword="null" /> the parameters <paramref name="requestedAllocationSize"/>, <paramref name="usedStorageSpace"/>,
        /// <paramref name="availableStorageSpace"/> and <paramref name="storageSpaceUsageLimit"/> refers to the storage provider; otherwise to the allocation token.
        /// </remarks>
        public StorageSpaceAllocationException(
            long requestedAllocationSize, 
            long usedStorageSpace, 
            long availableStorageSpace, 
            long storageSpaceUsageLimit, 
            Guid? allocationTokenId = null)
            : this(
                message: "Unable to allocate storage space.", 
                requestedAllocationSize, 
                usedStorageSpace, 
                availableStorageSpace, 
                storageSpaceUsageLimit, 
                allocationTokenId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageSpaceAllocationException"/> class with a specified error message and information about the storage space.
        /// </summary>
        /// <param name="message">
        /// The message that describes the allocation error.
        /// </param>
        /// <param name="requestedAllocationSize">
        /// Amount of bytes that should be allocated.
        /// </param>
        /// <param name="usedStorageSpace">
        /// How many bytes of the storage space where the resources should be allocated already were used.
        /// </param>
        /// <param name="availableStorageSpace">
        /// How many bytes of the storage space where the resources should be allocated were available.
        /// </param>
        /// <param name="storageSpaceUsageLimit">
        /// The maximum amount of bytes that were allowed to allocate. 
        /// </param>
        /// <param name="allocationTokenId">
        /// The id of the allocation token from which the associated resources should be allocated; <see langword="null" /> if
        /// the common resources of the storage prover should be allocated.
        /// </param>
        /// <remarks>
        /// If <paramref name="allocationTokenId"/> is <see langword="null" /> the parameters <paramref name="requestedAllocationSize"/>, <paramref name="usedStorageSpace"/>,
        /// <paramref name="availableStorageSpace"/> and <paramref name="storageSpaceUsageLimit"/> refers to the storage provider; otherwise to the allocation token.
        /// </remarks>
        public StorageSpaceAllocationException(
            string message, 
            long requestedAllocationSize, 
            long usedStorageSpace, 
            long availableStorageSpace, 
            long storageSpaceUsageLimit, 
            Guid? allocationTokenId = null)
            : base(GetExceptionMessage(
                message, 
                requestedAllocationSize, 
                usedStorageSpace, 
                availableStorageSpace, 
                storageSpaceUsageLimit, 
                allocationTokenId))
        {
            RequestedAllocationSize = requestedAllocationSize;
            UsedStorageSpace = usedStorageSpace;
            AvailableStorageSpace = availableStorageSpace;
            StorageSpaceUsageLimit = storageSpaceUsageLimit;
            AllocationTokenId = allocationTokenId;
        }

        private static string GetExceptionMessage(string message, long requestedAllocationSize, long usedStorageSpace, long availableStorageSpace, long storageSpaceUsageLimit, Guid? allocationTokenId = null)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(message)
                         .Append("RequestedAllocationSize: ").AppendLine(requestedAllocationSize.ToString())
                         .Append("UsedStorageSpace: ").AppendLine(usedStorageSpace.ToString())
                         .Append("AvailableStorageSpace: ").AppendLine(availableStorageSpace.ToString())
                         .Append("StorageSpaceUsageLimit: ").Append(storageSpaceUsageLimit.ToString());

            if (allocationTokenId != null)
                stringBuilder.AppendLine().Append("AllocationTokenId: ").Append(allocationTokenId.Value);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the id of the allocation token from which the associated resources should be allocated; <see langword="null" /> if
        /// the common resources of the storage prover should be allocated.
        /// </summary>
        public Guid? AllocationTokenId { get; }

        /// <summary>
        /// Gets the amount of bytes that should be allocated.
        /// </summary>
        public long RequestedAllocationSize { get; }

        /// <summary>
        /// Gets how many bytes of the storage space where the resources should be allocated already were used.
        /// </summary>
        /// <remarks>
        /// If <see cref="AllocationTokenId"/> is <see langword="null" /> the <see cref="UsedStorageSpace"/> refers to the storage provider; otherwise to the allocation token.
        /// </remarks>
        public long UsedStorageSpace { get; }

        /// <summary>
        /// Gets how many bytes of the storage space where the resources should be allocated were available.
        /// </summary>
        /// <remarks>
        /// If <see cref="AllocationTokenId"/> is <see langword="null" /> the <see cref="AvailableStorageSpace"/> refers to the storage provider; otherwise to the allocation token.
        /// </remarks>
        public long AvailableStorageSpace { get; }

        /// <summary>
        /// Gets the maximum amount of bytes that were allowed to allocate. 
        /// </summary>
        /// <remarks>
        /// If <see cref="AllocationTokenId"/> is <see langword="null" /> the <see cref="StorageSpaceUsageLimit"/> refers to the storage provider; otherwise to the allocation token.
        /// </remarks>
        public long StorageSpaceUsageLimit { get; }
    }
}
