using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static MyTorrent.HashingServiceProviders.HashUtils;

namespace MyTorrent.HashingServiceProviders
{
    /// <summary>
    /// Implements the standard <see cref="HashAlgorithm"/>.
    /// </summary>
    public class StandardHashingServiceProvider : IHashingServiceProvider, IDisposable
    {
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly HashAlgorithm _hashAlgorithm;
        
        /// <summary>
        /// Initializes a new <see cref="StandardHashingServiceProvider"/> instance.
        /// </summary>
        /// <param name="options"></param>
        public StandardHashingServiceProvider(IOptions<StandardHashingServiceProviderOptions>? options = null)
        {
            options ??= Options.Create(new StandardHashingServiceProviderOptions());

            _hashAlgorithmName = new HashAlgorithmName(options.Value?.HashAlgorithm ?? "SHA256");
            _hashAlgorithm = HashAlgorithm.Create(_hashAlgorithmName.Name);

            HashValueLength = _hashAlgorithm.HashSize / 4;
        }

        /// <summary>
        /// The length of the <see langword="string"/> representation of an hash value. 
        /// </summary>
        public int HashValueLength { get; }

        /// <summary>
        /// Gets the underlying <see langword="string"/> representation of the algorithm name.
        /// </summary>
        public string AlgorithmName => _hashAlgorithmName.Name;

        /// <summary>
        /// Computes the hash value for the specified byte array.
        /// </summary>
        /// <param name="buffer">
        /// The input to compute the hash value for.
        /// </param>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="buffer"/> is null.
        /// </exception>
        public string ComputeHash(byte[] buffer)
        {
            return ConvertByteHashToStringHash(_hashAlgorithm.ComputeHash(buffer));
        }

        /// <summary>
        /// Computes the hash value for the specified region of the specified byte array.
        /// </summary>
        /// <param name="buffer">
        /// The input to compute the hash value for.
        /// </param>
        /// <param name="offset">
        /// The offset into the byte array from which to begin using data.
        /// </param>
        /// <param name="count">
        /// The number of bytes in the array to use as data.
        /// </param>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws if <paramref name="offset"/> is out of range. This parameter requires a non-negative number.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws if <paramref name="count"/> is an invalid value. -or- <paramref name="buffer"/> length is invalid.
        /// </exception>
        public string ComputeHash(byte[] buffer, int offset, int count)
        {
            return ConvertByteHashToStringHash(_hashAlgorithm.ComputeHash(buffer, offset, count));
        }

        /// <summary>
        /// Computes the hash value for the specified <see cref="Stream"/> object.
        /// </summary>
        /// <param name="stream">
        /// The input to compute the hash code for.
        /// </param>
        /// <returns>
        /// <see langword="string"/> representation of the computed hash value.
        /// </returns>
        public string ComputeHash(Stream stream)
        {
            return ConvertByteHashToStringHash(_hashAlgorithm.ComputeHash(stream));
        }

        /// <summary>
        /// Gets an <see cref="IHashingServiceProvider"/> to incrementaly calculate a hash value.
        /// </summary>
        /// <returns>
        /// An <see cref="IIncrementalHashCalculator"/> implementation that calculates the hash the same way as this <see cref="IHashingServiceProvider"/>.
        /// </returns>
        public IIncrementalHashCalculator GetIncrementalHashCalculator()
        {
            return new StandardIncrementalHashCalculator(_hashAlgorithmName);
        }

        /// <summary>
        /// Converts a hash value to a normalized format.
        /// </summary>
        /// <param name="hash">
        /// The non-normalized hash value to normalize.
        /// </param>
        /// <returns>
        /// The normalized hash value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="hash"/> is <see langword="null"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Normalize(string hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            return hash.ToUpper();
        }

        /// <summary>
        /// Checks if a hash value is in a normalized format.
        /// </summary>
        /// <param name="hash">
        /// The hash value to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a hash value is in a normalized format; otherwise <see langword="false"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Validate(string hash)
        {
            //21.01.2020
            //I know what you think: "Why the heck didn't he used RegEx to implement this?"
            //Because Regex is a huge pile of garbage...
            //Now you think: "This idiot just don't know how to use RegEx." Yeah, realy? Try it yourself...
            //I used: new Regex("^[0-9A-Fa-f]{" + HashValueLength + "}$", RegexOptions.Compiled)
            //For some reason this bullshit recognizes "D9B5F58F0B38198293971865A14074F59EBA3E82595BECBE86AE51F1D9F1F65E\n" as a match.
            //I mean why the hack do I bother to put a '$' at the end when this crap F***ING IGNORES IT.
            //
            // P.S.: @Microsoft "The ^ and $ language elements match the beginning and end of the input string." MY ASS!!!! 
            //(Source: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options?view=netframework-4.8#default-options)

            //22.01.2020
            //Through the power of the Internet we discovered that '$' matches by default the end of the string input or '\n' before the end of the string input.
            //To match just the end of the string input you have to use '\z'. (As documented here: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference?view=netframework-4.8#anchors)
            //Everytime i think that i understood regex a situation occurs that clearly shows that i don't. Some day! (https://xkcd.com/208/)
            //The correct Regex would be: new Regex("^[0-9A-Fa-f]{" + HashValueLength + "}\\z", RegexOptions.Compiled)
            //
            //The reason it works this way is because this is how Perl-Compatible Regular Expressions 
            //(PCRE; the Regexp syntax that's basically the standard) were designed to work: (https://www.pcre.org/current/doc/html/pcre2syntax.html#SEC10)
            //
            //    $      end of subject  
            //             also before newline at end of subject
            //
            //And the reason Perl did regular expressions like this is because a common use case for 
            //regular expressions in Perl is to iterate over the lines of a file and test one or more 
            //regular expressions over each line, and the looping construct in Perl to iterate over a 
            //file's lines includes the \n at the end of the line in the loop variable for each 
            //iteration through the loop.
            //
            //Soo... it turns out not RegEx is a huge pile of garbage but JavaScript is. (I think this surprised exactly noone)
            //JavaScript is the odd exception where '$' means just the end of the input string.
            //Maybe you ask: Why?... Because it's Javascript! (It's always JavaScript)
            //
            //A tip for everyone out there: If you use RegEx Testers like "https://regexr.com", be sure that you set it to PCRE!!!
            //Otherwise you maybe find yourself in a Situation where you are the writer of such comment;)
            //
            //However, i don't go back to regex, because it is slower...
            //https://github.com/dominikviererbe/HashValueFormatValidationPerformance
            //
            //P.S.: I think i owe Microsoft an apology. They did nothing wrong and i am an idiot who just don't know how to use RegEx... ,but to my defence the doumentation was a bit missleading. (https://github.com/dotnet/docs/issues/16778)
            //      At this point i want to mention a funny comment of someone: "The plural of regex is regrets." :D

            if (hash == null || hash.Length != HashValueLength)
                return false;

            //For all LINQ fetishists out there... yes 'hash.All(c => "0123456789ABCDEFabcdef".Contains(c))' would have been valid too, but it is way slower... by more than 2x
            foreach (char c in hash.ToCharArray())
            {
                //checks if c is none of the following characters: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, a, b, c, d, e, f
                if ((c < '0' || c > '9') && (c < 'A' || c > 'F') && (c < 'a' || c > 'f'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="StandardHashingServiceProvider"/>.
        /// </summary>
        public void Dispose()
        {
            _hashAlgorithm.Dispose();
        }
    }
}
