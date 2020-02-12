using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MyTorrent.FragmentStorageProviders.Tests
{
    public class StorageSpaceAllocationExceptionTests
    {
        [Fact]
        public void StorageSpaceAllocationExceptionConstructor_Test1()
        {
            StorageSpaceAllocationException exception = new StorageSpaceAllocationException(
                requestedAllocationSize: 1L,
                usedStorageSpace: 2L,
                availableStorageSpace: 3L,
                storageSpaceUsageLimit: 4L);

            Assert.Null(exception.AllocationTokenId);
            Assert.Equal(1L, exception.RequestedAllocationSize);
            Assert.Equal(2L, exception.UsedStorageSpace);
            Assert.Equal(3L, exception.AvailableStorageSpace);
            Assert.Equal(4L, exception.StorageSpaceUsageLimit);

            string expectedExceptionMessage = "Unable to allocate storage space." + Environment.NewLine
                                            + "RequestedAllocationSize: 1" + Environment.NewLine
                                            + "UsedStorageSpace: 2" + Environment.NewLine
                                            + "AvailableStorageSpace: 3" + Environment.NewLine
                                            + "StorageSpaceUsageLimit: 4";

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void StorageSpaceAllocationExceptionConstructor_Test2()
        {
            Guid allocationTokenId = Guid.NewGuid();

            StorageSpaceAllocationException exception = new StorageSpaceAllocationException(
                requestedAllocationSize: 1L,
                usedStorageSpace: 2L,
                availableStorageSpace: 3L,
                storageSpaceUsageLimit: 4L,
                allocationTokenId);

            Assert.Equal(allocationTokenId, exception.AllocationTokenId);
            Assert.Equal(1L, exception.RequestedAllocationSize);
            Assert.Equal(2L, exception.UsedStorageSpace);
            Assert.Equal(3L, exception.AvailableStorageSpace);
            Assert.Equal(4L, exception.StorageSpaceUsageLimit);

            string expectedExceptionMessage = "Unable to allocate storage space." + Environment.NewLine
                                            + "RequestedAllocationSize: 1" + Environment.NewLine
                                            + "UsedStorageSpace: 2" + Environment.NewLine
                                            + "AvailableStorageSpace: 3" + Environment.NewLine
                                            + "StorageSpaceUsageLimit: 4" + Environment.NewLine
                                            + "AllocationTokenId: " + allocationTokenId.ToString();

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void StorageSpaceAllocationExceptionConstructor_Test3()
        {
            string errorMessage = "Lorem ipsum dolor amet.";

            StorageSpaceAllocationException exception = new StorageSpaceAllocationException(
                message: errorMessage,
                requestedAllocationSize: 1L,
                usedStorageSpace: 2L,
                availableStorageSpace: 3L,
                storageSpaceUsageLimit: 4L);

            Assert.Null(exception.AllocationTokenId);
            Assert.Equal(1L, exception.RequestedAllocationSize);
            Assert.Equal(2L, exception.UsedStorageSpace);
            Assert.Equal(3L, exception.AvailableStorageSpace);
            Assert.Equal(4L, exception.StorageSpaceUsageLimit);

            string expectedExceptionMessage = errorMessage + Environment.NewLine
                                            + "RequestedAllocationSize: 1" + Environment.NewLine
                                            + "UsedStorageSpace: 2" + Environment.NewLine
                                            + "AvailableStorageSpace: 3" + Environment.NewLine
                                            + "StorageSpaceUsageLimit: 4";
            
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void StorageSpaceAllocationExceptionConstructor_Test4()
        {
            Guid allocationTokenId = Guid.NewGuid();
            string errorMessage = "Lorem ipsum dolor amet.";

            StorageSpaceAllocationException exception = new StorageSpaceAllocationException(
                message: errorMessage,
                requestedAllocationSize: 1L,
                usedStorageSpace: 2L,
                availableStorageSpace: 3L,
                storageSpaceUsageLimit: 4L,
                allocationTokenId);

            Assert.Equal(allocationTokenId, exception.AllocationTokenId);
            Assert.Equal(1L, exception.RequestedAllocationSize);
            Assert.Equal(2L, exception.UsedStorageSpace);
            Assert.Equal(3L, exception.AvailableStorageSpace);
            Assert.Equal(4L, exception.StorageSpaceUsageLimit);

            string expectedExceptionMessage = errorMessage + Environment.NewLine
                                            + "RequestedAllocationSize: 1" + Environment.NewLine
                                            + "UsedStorageSpace: 2" + Environment.NewLine
                                            + "AvailableStorageSpace: 3" + Environment.NewLine
                                            + "StorageSpaceUsageLimit: 4" + Environment.NewLine
                                            + "AllocationTokenId: " + allocationTokenId.ToString();

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }
    }
}
