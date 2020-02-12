using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.HashingServiceProviders
{
    internal static class HashUtils
    {
        /// <summary>
        /// Converts a byte array to it's <see cref="string"/> representation which uses hexadecimal format.
        /// </summary>
        /// <param name="hash">
        /// The hash as byte array.
        /// </param>
        /// <returns>
        /// <see cref="string"/> representation of an hash as byte array which uses hexadecimal format.
        /// </returns>
        public static string ConvertByteHashToStringHash(byte[] hash)
        {
            StringBuilder hashString = new StringBuilder();

            for (int i = 0; i < hash.Length; ++i)
            {
                hashString.Append(hash[i].ToString("X2"));
            }

            return hashString.ToString();
        }
    }
}
