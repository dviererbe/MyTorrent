using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace MyTorrent.HashingServiceProviders.Tests
{
    public abstract class AbstractHashingServiceProviderTests
    {
        protected abstract IHashingServiceProvider GetInstance();

        protected abstract string AlgorithmName { get; }

        protected abstract int HashValueLength { get; }

        protected abstract IEnumerable<string> ValidHashValues { get; }

        protected abstract IEnumerable<string> InvalidHashValues { get; }

        protected abstract IEnumerable<(string NonNormalizedHashValue, string NormalizedHashValue)> NonNormalizedWithNormalizedHashValues { get; }

        protected abstract IEnumerable<(byte[] ByteData, string NormalizedHashValue)> ByteDataWithCorrectHashValue { get; }

        [Fact]
        public void AlgorithmName_Should_EqualCorrectHashingAlgorithmName()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Equal(AlgorithmName, hashingServiceProvider.AlgorithmName);
        }

        [Fact]
        public void HashValueLength_Should_EqualCorrectHashValueLength()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Equal(HashValueLength, hashingServiceProvider.HashValueLength);
        }

        [Fact]
        public void Validate_Should_ReturnTrue_When_HashIsValid()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var validHashValue in ValidHashValues)
            {
                Assert.True(hashingServiceProvider.Validate(validHashValue), $"(Valid) Hash-Value: '{validHashValue ?? "null"}'");
                Assert.Equal(validHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }

        [Fact]
        public void Validate_Should_ReturnFalse_When_HashIsNull()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.False(hashingServiceProvider.Validate(null));
        }

        [Fact]
        public void Validate_Should_ReturnFalse_When_HashIsInvalid()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var invalidHashValue in InvalidHashValues)
            {
                Assert.False(hashingServiceProvider.Validate(invalidHashValue), $"(Invalid) Hash-Value: '{invalidHashValue ?? "null"}'");
            }
        }

        [Fact]
        public void Normalize_Should_ReturnCorrectNormalizedHashValue()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var (NonNormalizedHashValue, NormalizedHashValue) in NonNormalizedWithNormalizedHashValues)
            {
                Assert.Equal(NormalizedHashValue, hashingServiceProvider.Normalize(NonNormalizedHashValue));
                Assert.Equal(NormalizedHashValue, hashingServiceProvider.Normalize(NormalizedHashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }

        [Fact]
        public void Normalize_Should_Throw_ArgumentNullException_When_HashIsNull()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Throws<ArgumentNullException>(() => hashingServiceProvider.Normalize(null));
        }

        [Fact]
        public void ComputeHash_Should_ReturnCorrectHashValue()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                string hashValue = hashingServiceProvider.ComputeHash(ByteData);

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);

                MemoryStream dataStream = new MemoryStream(ByteData, false);
                hashValue = hashingServiceProvider.ComputeHash(dataStream);

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }

        [Fact]
        public void ComputeHash_Should_ReturnCorrectHashValue_When_RegionIsSpecified()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                string hashValue = hashingServiceProvider.ComputeHash(ByteData, 0, ByteData.Length);

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }

        [Fact]
        public void ComputeHash_Should_Throw_ArgumentNullException_When_BufferIsNull()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Throws<ArgumentNullException>(() => hashingServiceProvider.ComputeHash(buffer: null));
        }

        [Fact]
        public void ComputeHash_Should_Throw_ArgumentNullException_When_BufferIsNull_And_RegionIsSpecified()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Throws<ArgumentNullException>(() => hashingServiceProvider.ComputeHash(null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => hashingServiceProvider.ComputeHash(null, 0, 1));
            Assert.Throws<ArgumentNullException>(() => hashingServiceProvider.ComputeHash(null, 0, 2));
            Assert.Throws<ArgumentNullException>(() => hashingServiceProvider.ComputeHash(null, 1, 2));
        }

        [Fact]
        public void ComputeHash_Should_Throw_ArgumentOutOfRangeException_When_OffsetIsOutOfRange()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Throws<ArgumentOutOfRangeException>(() => hashingServiceProvider.ComputeHash(Array.Empty<byte>(), -1, 0));

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => hashingServiceProvider.ComputeHash(ByteData, -1, ByteData.Length));
            }
        }

        [Fact]
        public void ComputeHash_Should_Throw_ArgumentException_When_CountIsAnInvalidValue()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            Assert.Throws<ArgumentException>(() => hashingServiceProvider.ComputeHash(Array.Empty<byte>(), 0, -1));
            Assert.Throws<ArgumentException>(() => hashingServiceProvider.ComputeHash(Array.Empty<byte>(), 0, 1));
            Assert.Throws<ArgumentException>(() => hashingServiceProvider.ComputeHash(Array.Empty<byte>(), 0, 10));

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                Assert.Throws<ArgumentException>(() => hashingServiceProvider.ComputeHash(ByteData, 0, -1));
                Assert.Throws<ArgumentException>(() => hashingServiceProvider.ComputeHash(ByteData, 0, ByteData.Length + 1));
                Assert.Throws<ArgumentException>(() => hashingServiceProvider.ComputeHash(ByteData, 0, ByteData.Length + 10));
            }
        }

        [Fact]
        public void IncrementalHashCalculator_Should_IncrementalyComputeHashValueCorrectly_Test1()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                using IIncrementalHashCalculator hashCalculator = hashingServiceProvider.GetIncrementalHashCalculator();

                hashCalculator.AppendData(ByteData);

                string hashValue = hashCalculator.GetHashAndReset();

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);

                foreach(byte b in ByteData)
                {
                    hashCalculator.AppendData(new byte[] { b });
                }

                hashValue = hashCalculator.GetHashAndReset();

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }

        [Fact]
        public void IncrementalHashCalculator_Should_IncrementalyComputeHashValueCorrectly_Test2()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                using IIncrementalHashCalculator hashCalculator = hashingServiceProvider.GetIncrementalHashCalculator();

                hashCalculator.AppendData(ByteData);

                string hashValue = hashCalculator.GetHashAndReset();

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);

                for (int i = 0; i < ByteData.Length; ++i)
                {
                    hashCalculator.AppendData(ByteData, i, 1);
                }

                hashValue = hashCalculator.GetHashAndReset();

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }

        [Fact]
        public void IncrementalHashCalculator_Should_IncrementalyComputeHashValueCorrectly_Test3()
        {
            IHashingServiceProvider hashingServiceProvider = GetInstance();

            foreach (var (ByteData, NormalizedHashValue) in ByteDataWithCorrectHashValue)
            {
                using IIncrementalHashCalculator hashCalculator = hashingServiceProvider.GetIncrementalHashCalculator();

                hashCalculator.AppendData(ByteData);

                string hashValue = hashCalculator.GetHashAndReset();

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);

                for (int i = 0; i < ByteData.Length; ++i)
                {
                    hashCalculator.AppendData(new byte[] { ByteData[i] }, 0, 1);
                }

                hashValue = hashCalculator.GetHashAndReset();

                Assert.Equal(NormalizedHashValue, hashValue);
                Assert.True(hashingServiceProvider.Validate(hashValue));
                Assert.Equal(hashValue, hashingServiceProvider.Normalize(hashValue));
                Assert.Equal(NormalizedHashValue.Length, hashingServiceProvider.HashValueLength);
            }
        }
    }
}
